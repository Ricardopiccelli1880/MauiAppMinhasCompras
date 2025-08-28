using MauiAppMinhasCompras.Helpers;
using MauiAppMinhasCompras.Models;
using System.Collections.ObjectModel; // <-- NECESSÁRIO PARA OBSERVABLECOLLECTION
using System.Globalization;

namespace MauiAppMinhasCompras;

public partial class MainPage : ContentPage
{
    private readonly SQLiteDatabaseHelper _db;
    private Produto? _editando;

    // NOVO: Lista que guarda TODOS os produtos do banco. É a nossa fonte de dados "mestra".
    private List<Produto> _todosOsProdutos = new();

    // NOVO: Coleção que a interface (CollectionView) observa. Contém apenas os itens filtrados.
    public ObservableCollection<Produto> ProdutosVisiveis { get; set; } = new();

    // NOVO: Propriedade para calcular o total APENAS dos itens visíveis/filtrados.
    public decimal TotalVisivel => ProdutosVisiveis.Sum(p => p.Quantidade * p.Preco);
    public string BotaoSalvarTexto => _editando is null ? "Salvar" : "Atualizar";

    private CancellationTokenSource? _buscaCts;

    public MainPage()
    {
        InitializeComponent();
        var dbPath = Path.Combine(FileSystem.AppDataDirectory, "produtos.db3");
        _db = new SQLiteDatabaseHelper(dbPath);
        BindingContext = this;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await CarregarDadosDoBancoAsync(); // Carrega os dados na lista mestra e aplica o filtro
    }

    // NOVO MÉTODO: Carrega os dados do banco para a lista mestra e atualiza a lista visível.
    private async Task CarregarDadosDoBancoAsync()
    {
        _todosOsProdutos = await _db.GetAllAsync();
        FiltrarProdutosNaMemoria(SearchBar.Text); // Filtra os dados com base no texto atual da busca
    }

    // MÉTODO DE BUSCA REESCRITO: Agora ele não acessa mais o banco de dados.
    private async void OnBuscarTextChanged(object sender, TextChangedEventArgs e)
    {
        _buscaCts?.Cancel();
        var cts = _buscaCts = new CancellationTokenSource();
        try
        {
            await Task.Delay(250, cts.Token); // Seu debounce continua aqui, perfeito!
            FiltrarProdutosNaMemoria(e.NewTextValue);
        }
        catch (TaskCanceledException)
        {
            // ignorado
        }
    }

    // NOVO MÉTODO: O coração da busca em memória.
    private void FiltrarProdutosNaMemoria(string? query)
    {
        var textoBusca = query?.Trim()?.ToLowerInvariant() ?? string.Empty;

        var produtosFiltrados = string.IsNullOrWhiteSpace(textoBusca)
            ? _todosOsProdutos // Se a busca for vazia, mostra todos
            : _todosOsProdutos.Where(p =>
                p.Descricao.ToLowerInvariant().Contains(textoBusca)
              ).ToList();

        ProdutosVisiveis.Clear();
        foreach (var produto in produtosFiltrados)
        {
            ProdutosVisiveis.Add(produto);
        }

        OnPropertyChanged(nameof(TotalVisivel)); // Notifica a UI para atualizar o total
    }

    private async void OnSalvarClicked(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(DescricaoEntry.Text) ||
            !int.TryParse(QuantidadeEntry.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var qtde) ||
            !TryParseDecimal(PrecoEntry.Text, out var preco))
        {
            await DisplayAlert("Atenção", "Preencha descrição, quantidade e preço válidos.", "OK");
            return;
        }

        if (_editando is null)
        {
            var novo = new Produto
            {
                Descricao = DescricaoEntry.Text!.Trim(),
                Quantidade = qtde,
                Preco = preco
            };
            await _db.InsertAsync(novo);
        }
        else
        {
            _editando.Descricao = DescricaoEntry.Text!.Trim();
            _editando.Quantidade = qtde;
            _editando.Preco = preco;
            await _db.UpdateAsync(_editando);
        }

        OnLimparCampos();
        await CarregarDadosDoBancoAsync(); // Recarrega a lista mestra após salvar
    }

    private async void OnExcluirClicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is Produto p)
        {
            var ok = await DisplayAlert("Excluir", $"Remover \"{p.Descricao}\"?", "Sim", "Não");
            if (!ok) return;
            await _db.DeleteAsync(p.Id);
            await CarregarDadosDoBancoAsync(); // Recarrega a lista mestra após excluir
        }
    }

    private async void OnResetarBancoClicked(object sender, EventArgs e)
    {
        var ok = await DisplayAlert("Confirmar", "Zerar a tabela de produtos?", "Sim", "Não");
        if (!ok) return;
        await _db.ResetAsync();
        OnLimparCampos();
        await CarregarDadosDoBancoAsync(); // Recarrega a lista mestra após resetar
    }

    private async void OnAdicionarExemploClicked(object sender, EventArgs e)
    {
        var p = new Produto { Descricao = "Arroz 5kg", Quantidade = 1, Preco = 27.90m };
        await _db.InsertAsync(p);
        await CarregarDadosDoBancoAsync(); // Recarrega a lista mestra após adicionar
    }

    // Este método agora simplesmente chama o CarregarDadosDoBancoAsync
    private async void OnRecarregarClicked(object sender, EventArgs e) => await CarregarDadosDoBancoAsync();

    // MÉTODOS AUXILIARES (sem alteração)
    private void OnLimparCampos()
    {
        _editando = null;
        DescricaoEntry.Text = string.Empty;
        QuantidadeEntry.Text = string.Empty;
        PrecoEntry.Text = string.Empty;
        OnPropertyChanged(nameof(BotaoSalvarTexto));
    }

    private void OnLimparClicked(object sender, EventArgs e) => OnLimparCampos();

    private void ProdutosListView_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection?.FirstOrDefault() is Produto p)
        {
            _editando = p;
            DescricaoEntry.Text = p.Descricao;
            QuantidadeEntry.Text = p.Quantidade.ToString(CultureInfo.InvariantCulture);
            PrecoEntry.Text = p.Preco.ToString("0.##", new CultureInfo("pt-BR"));
            OnPropertyChanged(nameof(BotaoSalvarTexto));
        }
        if (sender is CollectionView collectionView)
        {
            collectionView.SelectedItem = null;
        }
    }

    private static bool TryParseDecimal(string? input, out decimal value)
    {
        value = 0m;
        if (string.IsNullOrWhiteSpace(input)) return false;
        if (decimal.TryParse(input, NumberStyles.Number, new CultureInfo("pt-BR"), out value))
            return true;
        var normalized = input.Replace(',', '.');
        return decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.InvariantCulture, out value);
    }
}