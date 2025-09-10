using MauiAppMinhasCompras.Helpers;
using MauiAppMinhasCompras.Models;
using System.Collections.ObjectModel;
using System.Globalization;

namespace MauiAppMinhasCompras;

public partial class MainPage : ContentPage
{
    private readonly SQLiteDatabaseHelper _db;
    private Produto? _editando;
    private List<Produto> _todosOsProdutos = new();
    public ObservableCollection<Produto> ProdutosVisiveis { get; set; } = new();
    public decimal TotalVisivel => ProdutosVisiveis.Sum(p => p.Quantidade * p.Preco);
    public string BotaoSalvarTexto => _editando is null ? "Salvar" : "Atualizar";
    private CancellationTokenSource? _buscaCts;

    public MainPage()
    {
        InitializeComponent();
        var dbPath = Path.Combine(FileSystem.AppDataDirectory, "produtos.db3");
        _db = new SQLiteDatabaseHelper(dbPath);
        BindingContext = this;

        DataCadastroPicker.Date = DateTime.Now;
        FiltroDataInicioPicker.Date = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
        FiltroDataFimPicker.Date = DateTime.Now;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await CarregarDadosDoBancoAsync();
    }

    private async Task CarregarDadosDoBancoAsync()
    {
        _todosOsProdutos = await _db.GetAllAsync();
        FiltrarProdutosNaMemoria(SearchBar.Text);
    }

    private async void OnBuscarTextChanged(object sender, TextChangedEventArgs e)
    {
        _buscaCts?.Cancel();
        var cts = _buscaCts = new CancellationTokenSource();
        try
        {
            await Task.Delay(250, cts.Token);
            FiltrarProdutosNaMemoria(e.NewTextValue);
        }
        catch (TaskCanceledException) { /* ignorado */ }
    }

    private void OnFiltroDataChanged(object sender, DateChangedEventArgs e)
    {
        if (_todosOsProdutos.Any())
        {
            FiltrarProdutosNaMemoria(SearchBar.Text);
        }
    }

    private void FiltrarProdutosNaMemoria(string? query)
    {
        var textoBusca = query?.Trim()?.ToLowerInvariant() ?? string.Empty;
        var dataInicio = FiltroDataInicioPicker.Date;
        var dataFim = FiltroDataFimPicker.Date;

        var produtosFiltrados = _todosOsProdutos.Where(p =>
            (p.DataCadastro.Date >= dataInicio.Date && p.DataCadastro.Date <= dataFim.Date) &&
            (string.IsNullOrWhiteSpace(textoBusca) || p.Descricao.ToLowerInvariant().Contains(textoBusca))
        ).ToList();

        ProdutosVisiveis.Clear();
        foreach (var produto in produtosFiltrados)
        {
            ProdutosVisiveis.Add(produto);
        }
        OnPropertyChanged(nameof(TotalVisivel));
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
                Preco = preco,
                DataCadastro = DataCadastroPicker.Date
            };
            await _db.InsertAsync(novo);
        }
        else
        {
            _editando.Descricao = DescricaoEntry.Text!.Trim();
            _editando.Quantidade = qtde;
            _editando.Preco = preco;
            _editando.DataCadastro = DataCadastroPicker.Date;
            await _db.UpdateAsync(_editando);
        }
        OnLimparCampos();
        await CarregarDadosDoBancoAsync();
    }

    private async void OnExcluirClicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is Produto p)
        {
            var ok = await DisplayAlert("Excluir", $"Remover \"{p.Descricao}\"?", "Sim", "Não");
            if (!ok) return;
            await _db.DeleteAsync(p.Id);
            await CarregarDadosDoBancoAsync();
        }
    }

    private async void OnResetarBancoClicked(object sender, EventArgs e)
    {
        var ok = await DisplayAlert("Confirmar", "Zerar a tabela de produtos?", "Sim", "Não");
        if (!ok) return;
        await _db.ResetAsync();
        OnLimparCampos();
        await CarregarDadosDoBancoAsync();
    }

    private async void OnAdicionarExemploClicked(object sender, EventArgs e)
    {
        var p = new Produto { Descricao = "Arroz 5kg", Quantidade = 1, Preco = 27.90m, DataCadastro = DateTime.Now };
        await _db.InsertAsync(p);
        await CarregarDadosDoBancoAsync();
    }

    private async void OnRecarregarClicked(object sender, EventArgs e) => await CarregarDadosDoBancoAsync();

    private void OnLimparCampos()
    {
        _editando = null;
        DescricaoEntry.Text = string.Empty;
        QuantidadeEntry.Text = string.Empty;
        PrecoEntry.Text = string.Empty;
        DataCadastroPicker.Date = DateTime.Now;
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
            DataCadastroPicker.Date = p.DataCadastro;
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