using SQLite;

namespace MauiAppMinhasCompras.Models
{
    [Table("Produto")]
    public class Produto
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        [NotNull]
        public string Descricao { get; set; } = string.Empty;

        public int Quantidade { get; set; }

        // Use decimal para valores monetários
        public decimal Preco { get; set; }
    }
}
