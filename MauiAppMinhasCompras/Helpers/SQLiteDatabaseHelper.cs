using SQLite;
using MauiAppMinhasCompras.Models;

namespace MauiAppMinhasCompras.Helpers
{
    public class SQLiteDatabaseHelper
    {
        private readonly SQLiteAsyncConnection _connection;

        public SQLiteDatabaseHelper(string dbPath)
        {
            _connection = new SQLiteAsyncConnection(dbPath);
            _connection.CreateTableAsync<Produto>().Wait();
        }

        // Método para pegar todos os produtos
        public async Task<List<Produto>> GetAllAsync()
        {
            return await _connection.Table<Produto>().ToListAsync();
        }

        // Método para buscar produtos por descrição
        public async Task<List<Produto>> SearchAsync(string query)
        {
            return await _connection.Table<Produto>()
                                     .Where(p => p.Descricao.Contains(query))
                                     .ToListAsync();
        }

        // Método para resetar a tabela de produtos (excluir todos os produtos)
        public async Task<int> ResetAsync()
        {
            return await _connection.DeleteAllAsync<Produto>();
        }

        // Método para inserir um produto
        public async Task<int> InsertAsync(Produto produto)
        {
            return await _connection.InsertAsync(produto);
        }

        // Método para atualizar um produto
        public async Task<int> UpdateAsync(Produto produto)
        {
            return await _connection.UpdateAsync(produto);
        }

        // Método para excluir um produto
        public async Task<int> DeleteAsync(int id)
        {
            return await _connection.DeleteAsync<Produto>(id);
        }
    }
}
