using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore;

namespace Kpz.Lab2.Data;

/// <summary>Виводить метадані моделі EF Core, налаштованої для контексту бази даних.</summary>
public static class EfModelDemo
{
    public static void Print(IModel model)
    {
        Console.WriteLine();
        Console.WriteLine("=== НАЛАШТУВАННЯ МОДЕЛІ EF CORE ===");

        foreach (var entity in model.GetEntityTypes().OrderBy(entity => entity.GetTableName()))
        {
            var tableName = entity.GetTableName() ?? entity.ClrType.Name;
            var primaryKey = entity.FindPrimaryKey();
            var keyNames = primaryKey is null
                ? "(no primary key)"
                : string.Join(", ", primaryKey.Properties.Select(property => property.Name));

            Console.WriteLine($"Таблиця {tableName}: первинний ключ=({keyNames})");

            foreach (var index in entity.GetIndexes().Where(index => index.IsUnique))
            {
                var indexColumns = string.Join(", ", index.Properties.Select(property => property.Name));
                Console.WriteLine($"  Унікальний індекс ({indexColumns})");
            }

            foreach (var foreignKey in entity.GetForeignKeys())
            {
                var columns = string.Join(", ", foreignKey.Properties.Select(property => property.Name));
                var principal = foreignKey.PrincipalEntityType.GetTableName() ?? foreignKey.PrincipalEntityType.ClrType.Name;
                Console.WriteLine(
                    $"  FK ({columns}) -> {principal}; DeleteBehavior={foreignKey.DeleteBehavior}");
            }
        }
    }
}
