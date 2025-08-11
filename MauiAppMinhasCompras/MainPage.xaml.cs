using MauiAppMinhasCompras.Helpers;
using MauiAppMinhasCompras.Models;
using System.Globalization;
using System.Linq;
using System.Threading;

namespace MauiAppMinhasCompras;

public partial class MainPage : ContentPage
{
    private readonly SQLiteDatabaseHelper _db;
    private Produto? _editando;

    public List<Produto> Produtos { get; set; } = new();
    public decimal Total => Produtos.Sum(p => p.Quantidade * p.Preco);

    public string BotaoSalvarTexto => _editando is null ? "Salvar" : "Atualizar";

    // Para debounce da busca
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
        await CarregarAsync();
    }

    private async Task CarregarAsync()
    {
        Produtos = await _db.GetAllAsync();
        OnPropertyChanged(nameof(Produtos));
        OnPropertyChanged(nameof(Total));
    }

    private void OnLimparCampos()
    {
        _editando = null;
        DescricaoEntry.Text = string.Empty;
        QuantidadeEntry.Text = string.Empty;
        PrecoEntry.Text = string.Empty;
        OnPropertyChanged(nameof(BotaoSalvarTexto));
    }

    private static bool TryParseDecimal(string? input, out decimal value)
    {
        value = 0m;
        if (string.IsNullOrWhiteSpace(input)) return false;

        // Tenta padrão brasileiro
        if (decimal.TryParse(input, NumberStyles.Number, new CultureInfo("pt-BR"), out value))
            return true;

        // Tenta convertendo vírgula -> ponto (teclado numérico)
        var normalized = input.Replace(',', '.');
        return decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.InvariantCulture, out value);
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
        await CarregarAsync();
    }

    private void OnLimparClicked(object sender, EventArgs e) => OnLimparCampos();

    private async void OnRecarregarClicked(object sender, EventArgs e) => await CarregarAsync();

    private async void OnAdicionarExemploClicked(object sender, EventArgs e)
    {
        var p = new Produto { Descricao = "Arroz 5kg", Quantidade = 1, Preco = 27.90m };
        await _db.InsertAsync(p);
        await CarregarAsync();
    }

    private async void OnResetarBancoClicked(object sender, EventArgs e)
    {
        var ok = await DisplayAlert("Confirmar", "Zerar a tabela de produtos?", "Sim", "Não");
        if (!ok) return;

        await _db.ResetAsync();
        OnLimparCampos();
        await CarregarAsync();
    }

    // Editar: toque no item
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

        // remove seleção
        ((CollectionView)sender!).SelectedItem = null;
    }

    // Excluir (Swipe)
    private async void OnExcluirSwipeInvoked(object sender, EventArgs e)
    {
        if (sender is SwipeItem swipe && swipe.CommandParameter is Produto p)
        {
            var ok = await DisplayAlert("Excluir", $"Remover \"{p.Descricao}\"?", "Sim", "Não");
            if (!ok) return;

            await _db.DeleteAsync(p.Id);
            await CarregarAsync();
        }
    }

    // Busca reativa com debounce
    private async void OnBuscarTextChanged(object sender, TextChangedEventArgs e)
    {
        _buscaCts?.Cancel();
        var cts = _buscaCts = new CancellationTokenSource();

        try
        {
            await Task.Delay(250, cts.Token); // debounce
            var q = e.NewTextValue?.Trim();

            Produtos = string.IsNullOrWhiteSpace(q)
                ? await _db.GetAllAsync()
                : await _db.SearchAsync(q);

            OnPropertyChanged(nameof(Produtos));
            OnPropertyChanged(nameof(Total));
        }
        catch (TaskCanceledException)
        {
            // ignorado
        }
    }
}
