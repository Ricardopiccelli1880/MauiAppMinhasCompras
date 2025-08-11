using SQLite;
using MauiAppMinhasCompras.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MauiAppMinhasCompras.Helpers
{
    public class SQLiteDatabaseHelper
    {
        private readonly SQLiteAsyncConnection _conn;

        public SQLiteDatabaseHelper(string dbPath)
        {
            _conn = new SQLiteAsyncConnection(dbPath);
            _conn.CreateTableAsync<Produto>().Wait(); // simples e direto para este projeto
        }

        public Task<List<Produto>> GetAllAsync()
            => _conn.Table<Produto>()
                     .OrderBy(p => p.Descricao)
                     .ToListAsync();

        public Task<int> InsertAsync(Produto p)
            => _conn.InsertAsync(p);

        public Task<int> UpdateAsync(Produto p)
            => _conn.UpdateAsync(p);

        // Forma mais direta pela PK
        public Task<int> DeleteAsync(int id)
            => _conn.DeleteAsync<Produto>(id);

        // Busca por descrição com LIKE parametrizado (case-insensitive)
        public Task<List<Produto>> SearchAsync(string q)
        {
            if (string.IsNullOrWhiteSpace(q))
                return GetAllAsync();

            // Escapa curingas do LIKE
            var pattern = $"%{q.Replace("%", "[%]")
                               .Replace("_", "[_]")
                               .Replace("[", "[[]")}%";

            return _conn.QueryAsync<Produto>(
                "SELECT * FROM Produto " +
                "WHERE Descricao LIKE ? COLLATE NOCASE " +
                "ORDER BY Descricao",
                pattern
            );
        }

        public async Task ResetAsync()
        {
            await _conn.DropTableAsync<Produto>();
            await _conn.CreateTableAsync<Produto>();
        }
    }
}
