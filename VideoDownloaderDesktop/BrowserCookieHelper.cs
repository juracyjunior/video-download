namespace VideoDownloaderDesktop;

internal static class BrowserCookieHelper
{
    public static bool HasVisibleBrowserWindow(string browser)
    {
        foreach (var process in GetBrowserProcesses(browser))
        {
            try
            {
                if (process.MainWindowHandle != IntPtr.Zero)
                {
                    return true;
                }
            }
            catch
            {
            }
            finally
            {
                process.Dispose();
            }
        }

        return false;
    }

    public static int GetBackgroundProcessCount(string browser)
    {
        var count = 0;

        foreach (var process in GetBrowserProcesses(browser))
        {
            try
            {
                if (process.MainWindowHandle == IntPtr.Zero)
                {
                    count++;
                }
            }
            catch
            {
            }
            finally
            {
                process.Dispose();
            }
        }

        return count;
    }

    public static string GetBrowserDisplayName(string browser)
    {
        return browser.ToLowerInvariant() switch
        {
            "chrome" => "Google Chrome",
            "edge" or "msedge" => "Microsoft Edge",
            "firefox" => "Mozilla Firefox",
            "brave" => "Brave",
            "opera" => "Opera",
            _ => browser
        };
    }

    public static bool TryGetCookieDatabaseErrorMessage(string output, string browser, out string message)
    {
        if (output.Contains("Could not copy", StringComparison.OrdinalIgnoreCase)
            && output.Contains("cookie database", StringComparison.OrdinalIgnoreCase))
        {
            var browserName = GetBrowserDisplayName(browser);
            message = $"Não foi possível ler os cookies do {browserName}. " +
                      $"Mesmo com o navegador fechado, o Windows pode manter processos em segundo plano. " +
                      $"Abra o Gerenciador de Tarefas, finalize todos os processos de \"{browserName}\" e tente novamente.";
            return true;
        }

        message = string.Empty;
        return false;
    }

    public static bool TryGetAuthErrorMessage(string output, string browser, bool usingCookiesFile, out string message)
    {
        var browserName = GetBrowserDisplayName(browser);

        if (output.Contains("empty media response", StringComparison.OrdinalIgnoreCase)
            || output.Contains("Instagram sent an empty", StringComparison.OrdinalIgnoreCase))
        {
            message = usingCookiesFile
                ? "O Instagram rejeitou a sessão dos cookies. Exporte novamente o cookies.txt estando logado em instagram.com (use a extensão Get cookies.txt LOCALLY no domínio do Instagram) e substitua lib\\cookies.txt."
                : $"O Instagram exige login. Feche o {browserName}, confirme que está logado no Instagram e tente novamente.";
            return true;
        }

        if (output.Contains("login required", StringComparison.OrdinalIgnoreCase)
            || output.Contains("--cookies-from-browser", StringComparison.OrdinalIgnoreCase)
            || output.Contains("--cookies for the authentication", StringComparison.OrdinalIgnoreCase))
        {
            message = usingCookiesFile
                ? "Este vídeo exige autenticação. Atualize lib\\cookies.txt com cookies válidos do site (incluindo sessionid do Instagram)."
                : $"Este vídeo exige autenticação. Feche o {browserName} e confirme que está logado no site no navegador selecionado.";
            return true;
        }

        message = string.Empty;
        return false;
    }

    public static bool TryGetInstagramNotFoundMessage(string output, bool usingCookiesFile, out string message)
    {
        if (output.Contains("[Instagram]", StringComparison.OrdinalIgnoreCase)
            && (output.Contains("HTTP Error 404", StringComparison.OrdinalIgnoreCase)
                || output.Contains("404: Not Found", StringComparison.OrdinalIgnoreCase)))
        {
            message = usingCookiesFile
                ? "Instagram retornou 404. Confirme se o vídeo existe e se os cookies em lib\\cookies.txt estão atualizados (sessionid válido)."
                : "Instagram retornou 404. Confirme se o vídeo existe e se você está logado no Instagram.";
            return true;
        }

        message = string.Empty;
        return false;
    }

    public static bool TryGetDpapiErrorMessage(string output, out string message)
    {
        if (output.Contains("Failed to decrypt with DPAPI", StringComparison.OrdinalIgnoreCase))
        {
            message = "O Windows bloqueou a leitura automática de cookies do Chrome/Edge. " +
                      "Exporte todos os cookies com a extensão \"Get cookies.txt LOCALLY\" e salve em lib\\cookies.txt, " +
                      "ou selecione Mozilla Firefox no app (funciona sem exportar).";
            return true;
        }

        message = string.Empty;
        return false;
    }

    private static IEnumerable<System.Diagnostics.Process> GetBrowserProcesses(string browser)
    {
        var processName = browser.ToLowerInvariant() switch
        {
            "chrome" => "chrome",
            "edge" or "msedge" => "msedge",
            "firefox" => "firefox",
            "brave" => "brave",
            "opera" => "opera",
            _ => browser
        };

        return System.Diagnostics.Process.GetProcessesByName(processName);
    }
}
