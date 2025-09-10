using System.Globalization; // ADICIONE ESTA LINHA para CultureInfo
using System.Threading;     // ADICIONE ESTA LINHA para Thread

namespace MauiAppMinhasCompras;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();

        // INÍCIO DO CÓDIGO DE REGIONALIZAÇÃO
        // Define a cultura como Português do Brasil ("pt-BR") para todo o aplicativo.
        // Isso garante que datas, moedas e números sejam formatados corretamente.
        var cultura = new CultureInfo("pt-BR");

        Thread.CurrentThread.CurrentCulture = cultura;
        Thread.CurrentThread.CurrentUICulture = cultura;
        // FIM DO CÓDIGO DE REGIONALIZAÇÃO

        MainPage = new AppShell();
    }
}