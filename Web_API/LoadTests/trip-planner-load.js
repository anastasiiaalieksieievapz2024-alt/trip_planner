import http from 'k6/http';
import { check, sleep } from 'k6';
import { Trend } from 'k6/metrics';

const baseUrl = (__ENV.BASE_URL || 'https://localhost:7198').replace(/\/$/, '');
const token = __ENV.AUTH_TOKEN;
const memberEmail = __ENV.TEST_MEMBER_EMAIL;
const tripCategoryId = Number(__ENV.TRIP_CATEGORY_ID || 1);
const itineraryItemTypeId = Number(__ENV.ITINERARY_ITEM_TYPE_ID || 1);
const resultFile = __ENV.RESULT_FILE || 'k6-summary.json';

if (!token) {
  throw new Error('Set AUTH_TOKEN to a fresh token with read/create/update/delete permissions for trips and itinerary, plus read/create/delete permissions for members.');
}
if (!memberEmail) {
  throw new Error('Set TEST_MEMBER_EMAIL to a different, already registered user. The script adds and removes this user on each temporary trip.');
}
if (!Number.isInteger(tripCategoryId) || tripCategoryId < 1 || !Number.isInteger(itineraryItemTypeId) || itineraryItemTypeId < 1) {
  throw new Error('TRIP_CATEGORY_ID and ITINERARY_ITEM_TYPE_ID must be positive integers that exist in the database.');
}

const endpointDuration = new Trend('endpoint_duration', true);
const authHeaders = {
  Accept: 'application/json',
  Authorization: `Bearer ${token}`,
};
const jsonParams = (endpoint) => ({
  headers: { ...authHeaders, 'Content-Type': 'application/json' },
  tags: { endpoint },
});
const getParams = (endpoint, authenticated = true) => ({
  headers: authenticated ? authHeaders : { Accept: 'application/json' },
  tags: { endpoint },
});

export const options = {
  // Development HTTPS uses a local certificate. Do not use this setting for production.
  insecureSkipTLSVerify: true,
  summaryTrendStats: ['avg', 'min', 'med', 'max', 'p(90)', 'p(95)', 'p(99)'],
  stages: [
    { duration: '20s', target: 5 },
    { duration: '40s', target: 5 },
    { duration: '20s', target: 10 },
    { duration: '40s', target: 10 },
    { duration: '20s', target: 0 },
  ],
  thresholds: {
    http_req_failed: ['rate<0.01'],
    http_req_duration: ['p(95)<500'],
  },
};

function send(endpoint, method, url, body, expectedStatus, authenticated = true) {
  const params = method === 'GET'
    ? getParams(endpoint, authenticated)
    : jsonParams(endpoint);
  const response = http.request(method, url, body === undefined ? null : JSON.stringify(body), params);
  endpointDuration.add(response.timings.duration, { endpoint });
  check(response, {
    [`${endpoint}: HTTP ${expectedStatus}`]: (res) => res.status === expectedStatus,
  });
  return response;
}

function isPagedResponse(response, endpoint) {
  if (response.status !== 200) return false;
  let data;
  try { data = response.json(); } catch (_) { return false; }
  return check(data, {
    [`${endpoint}: paged response shape`]: (value) =>
      Array.isArray(value.items) &&
      Number.isInteger(value.page) &&
      Number.isInteger(value.pageSize) &&
      Number.isInteger(value.totalCount) &&
      Number.isInteger(value.totalPages),
  });
}

function responseId(response, field) {
  if (response.status < 200 || response.status >= 300) return null;
  try {
    const id = response.json()[field];
    return Number.isInteger(id) && id > 0 ? id : null;
  } catch (_) {
    return null;
  }
}

function dateOnly(date) {
  return date.toISOString().slice(0, 10);
}

export default function () {
  // Read-only endpoints use the seeded sample trip (TripId 1).
  const readRequests = [
    ['trips_list', 'GET', `${baseUrl}/api/trips?page=1&pageSize=5&destination=%D0%9B%D1%8C%D0%B2%D1%96%D0%B2&sortBy=startDate&sortDirection=asc`],
    ['trip_details', 'GET', `${baseUrl}/api/trips/1`],
    ['trip_itinerary_list', 'GET', `${baseUrl}/api/trips/1/itinerary?page=1&pageSize=5&type=Flight&title=%D0%B0%D0%B5%D1%80%D0%BE%D0%BF%D0%BE%D1%80%D1%82&sortBy=startDateTime&sortDirection=asc`],
    ['members_list', 'GET', `${baseUrl}/api/trips/1/members?page=1&pageSize=10&search=na881&role=TripReader&sortBy=name&sortDirection=asc`],
    ['items_list', 'GET', `${baseUrl}/api/trips/1/items?page=1&pageSize=5&sortBy=startDateTime&sortDirection=asc`],
    ['test_public', 'GET', `${baseUrl}/api/test/public`, false],
    ['test_protected', 'GET', `${baseUrl}/api/test/protected`],
    ['test_planner', 'GET', `${baseUrl}/api/test/planner`],
  ];

  readRequests.forEach(([endpoint, method, url, authenticated = true]) => {
    const response = send(endpoint, method, url, undefined, 200, authenticated);
    if (['trips_list', 'trip_itinerary_list', 'members_list', 'items_list'].includes(endpoint)) {
      isPagedResponse(response, endpoint);
    }
    if (endpoint === 'trip_details' && response.status === 200) {
      let data;
      try { data = response.json(); } catch (_) { data = {}; }
      check(data, { 'trip_details: trip identifier is 1': (value) => value.tripId === 1 });
    }
  });

  // Exercise all write routes against a trip created by this iteration, then clean it up.
  let tripId = null;
  let itemId = null;
  let memberId = null;
  const now = new Date();
  const tripStart = new Date(now.getTime() - 60 * 60 * 1000);
  const tripEnd = new Date(now.getTime() + 24 * 60 * 60 * 1000);
  const itemStart = new Date(now.getTime() + 60 * 60 * 1000);
  const itemEnd = new Date(now.getTime() + 2 * 60 * 60 * 1000);
  const unique = `${__VU}-${__ITER}-${now.getTime()}`;
  const tripPayload = {
    tripName: `k6-load-${unique}`,
    destination: 'Load test',
    tripCategoryId,
    startDate: dateOnly(tripStart),
    endDate: dateOnly(tripEnd),
    description: 'Temporary k6 load-test record',
    planningNotes: 'Created and removed by trip-planner-load.js',
  };
  const itemPayload = {
    itineraryItemTypeId,
    title: `k6 item ${unique}`,
    description: 'Temporary k6 load-test record',
    startDateTime: itemStart.toISOString(),
    endDateTime: itemEnd.toISOString(),
    address: 'Load test location',
    latitude: 49.8397,
    longitude: 24.0297,
  };

  try {
    const createdTrip = send('trip_create', 'POST', `${baseUrl}/api/trips`, tripPayload, 201);
    tripId = responseId(createdTrip, 'tripId');
    check({ tripId }, { 'trip_create: response contains tripId': (value) => value.tripId !== null });

    if (tripId !== null) {
      send('trip_update', 'PUT', `${baseUrl}/api/trips/${tripId}`, {
        ...tripPayload,
        tripName: `k6-updated-${unique}`,
      }, 200);

      const createdItem = send('item_create', 'POST', `${baseUrl}/api/trips/${tripId}/items`, itemPayload, 201);
      itemId = responseId(createdItem, 'itemId');
      check({ itemId }, { 'item_create: response contains itemId': (value) => value.itemId !== null });

      const addedMember = send('member_create', 'POST', `${baseUrl}/api/trips/${tripId}/members`, { email: memberEmail }, 201);
      memberId = responseId(addedMember, 'userId');
      check({ memberId }, { 'member_create: response contains userId': (value) => value.memberId !== null });

      send('members_list_created_trip', 'GET', `${baseUrl}/api/trips/${tripId}/members?page=1&pageSize=10&sortBy=name&sortDirection=asc`, undefined, 200);

      if (itemId !== null) {
        send('item_details_created', 'GET', `${baseUrl}/api/trips/${tripId}/items/${itemId}`, undefined, 200);
        send('item_update', 'PUT', `${baseUrl}/api/trips/${tripId}/items/${itemId}`, {
          ...itemPayload,
          title: `k6 updated item ${unique}`,
        }, 200);
      }

      send('items_list_created_trip', 'GET', `${baseUrl}/api/trips/${tripId}/items?page=1&pageSize=10&sortBy=startDateTime&sortDirection=asc`, undefined, 200);
      send('trip_itinerary_created_trip', 'GET', `${baseUrl}/api/trips/${tripId}/itinerary?page=1&pageSize=10&sortBy=startDateTime&sortDirection=asc`, undefined, 200);

      if (memberId !== null) {
        send('member_delete', 'DELETE', `${baseUrl}/api/trips/${tripId}/members/${memberId}`, undefined, 204);
      }
      if (itemId !== null) {
        send('item_delete', 'DELETE', `${baseUrl}/api/trips/${tripId}/items/${itemId}`, undefined, 204);
      }
    }
  } finally {
    if (tripId !== null) {
      send('trip_delete', 'DELETE', `${baseUrl}/api/trips/${tripId}`, undefined, 204);
    }
  }

  sleep(1);
}

export function handleSummary(data) {
  return {
    [resultFile]: JSON.stringify(data, null, 2),
    stdout: `k6 summary saved to ${resultFile}\n`,
  };
}
