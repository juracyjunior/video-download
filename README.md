# SoViDow — Video Downloader

Aplicativo desktop para Windows que organiza e baixa vídeos a partir de URLs, usando o `yt-dlp` como mecanismo de extração. A interface permite enfileirar vários links, escolher a resolução desejada e acompanhar o resultado de cada item.

> Use o aplicativo somente para conteúdo que você tem autorização para baixar. O acesso a conteúdo privado ou restrito pode exigir uma sessão válida no site.

## Recursos

- Interface Windows Forms em português.
- Uma ou mais URLs HTTP/HTTPS, com uma URL por linha; links repetidos são removidos.
- Fila sequencial com estado por vídeo, resumo final e ação para tentar novamente itens com erro.
- Pasta de destino escolhida pelo usuário e lembrada entre execuções.
- Nome base configurável e numeração automática para downloads da fila.
- Resoluções solicitadas de 360p, 480p, 720p, 1080p, 1440p ou 2160p. O aplicativo escolhe a menor resolução disponível igual ou acima da solicitada; se não houver, usa a maior disponível abaixo dela.
- Seleção de formato com preferência por MP4, download direto e maior bitrate. Formatos sem áudio podem ser combinados com áudio disponível; a saída solicita contêiner MP4.
- Cookies lidos de `lib\cookies.txt` quando esse arquivo existe; na ausência dele, o app usa cookies do perfil selecionado do Chrome, Edge ou Firefox.
- Tratamentos específicos para YouTube e Instagram, incluindo novas tentativas em casos de autenticação e erro HTTP 403 no YouTube.
- Ajuste automático de URLs do Instagram no formato `/reels/` para `/reel/`.
- Verificação de conflito de arquivo antes de substituir um download existente.

## Requisitos

- Windows, necessário para executar o aplicativo Windows Forms.
- .NET 10 SDK para compilar a partir do código-fonte.
- `yt-dlp.exe` em `VideoDownloaderDesktop\lib\yt-dlp.exe`. O executável está incluído neste repositório.
- FFmpeg disponível para o `yt-dlp` quando o formato escolhido precisar combinar faixas separadas de vídeo e áudio.
- Para usar cookies do navegador, um perfil compatível instalado e uma sessão autenticada no site. Fechar o navegador antes do download pode ajudar quando o banco de cookies estiver bloqueado.

## Executar a partir do código

No PowerShell, na pasta raiz do repositório:

```powershell
dotnet restore .\VideoDownloader.slnx
dotnet run --project .\VideoDownloaderDesktop\VideoDownloaderDesktop.csproj
```

Para compilar em modo Release:

```powershell
dotnet build .\VideoDownloader.slnx --configuration Release
```

O projeto tem como alvo `net10.0-windows` e copia `appsettings.json` e o conteúdo da pasta `lib` para a saída da compilação. O caminho de `yt-dlp.exe` é relativo ao diretório do aplicativo (`lib\yt-dlp.exe`).

## Como usar

1. Cole os links dos vídeos na caixa **URLs dos vídeos**, um por linha.
2. Selecione uma pasta existente em **Pasta de destino**. Use **Selecionar...** para escolher uma pasta; a seleção é salva nas configurações.
3. Informe o nome base em **Nome do arquivo**, sem extensão. A extensão final é determinada pelo formato baixado.
4. Selecione a resolução desejada.
5. Se não houver arquivo `lib\cookies.txt`, escolha o navegador no qual está autenticado.
6. Clique em **Baixar**. A fila é processada sequencialmente e os detalhes aparecem no log.
7. Se um item falhar, use **Retentar** na linha correspondente.

Para nomes, se o nome base não terminar em número, a fila gera `video`, `video1`, `video2` etc. Se terminar em número, incrementa essa parte preservando zeros à esquerda: `react100`, `react101`, `react102`. Um único link usa exatamente o nome base informado.

## Cookies e autenticação

### Arquivo de cookies

O arquivo `VideoDownloaderDesktop\lib\cookies.txt` é usado automaticamente se existir; quando existe, ele tem precedência sobre os cookies do navegador. O caminho pode ser alterado pela propriedade `CookiesFile` em `appsettings.json` (caminhos relativos são resolvidos a partir da pasta do aplicativo).

O arquivo de cookies pode conter tokens de sessão que dão acesso às suas contas. Não o publique, não o envie a terceiros e não o inclua em commits compartilhados. Exporte cookies somente de contas que você controla e substitua o arquivo quando a sessão expirar. O arquivo presente no projeto é um dado local de autenticação e deve ser tratado como segredo.

### Cookies do navegador

Quando não há arquivo de cookies, o aplicativo passa ao `yt-dlp` o navegador e o perfil configurados. A interface permite selecionar Chrome, Edge ou Firefox. Feche o navegador antes de tentar novamente se a leitura do banco de cookies falhar; processos em segundo plano também podem manter o arquivo bloqueado.

No Windows, a descriptografia automática de cookies do Chrome/Edge pode falhar (DPAPI). Nesse caso, use Firefox ou configure um arquivo `cookies.txt` válido. Para conteúdo que exige login, confirme que a sessão exportada ainda está autenticada no site.

## Configuração

Arquivo: `VideoDownloaderDesktop\appsettings.json`.

```json
{
  "Version": "26.09.17-01",
  "CookiesFromBrowser": "chrome",
  "CookiesBrowserProfile": "Default",
  "CookiesFile": "lib\\cookies.txt"
}
```

| Propriedade | Descrição |
| --- | --- |
| `Version` | Texto da versão exibida na barra inferior da janela. |
| `LastOutputFolder` | Última pasta de destino escolhida; gravada pelo aplicativo. |
| `CookiesFromBrowser` | Navegador usado quando não existe arquivo de cookies (`chrome`, `edge` ou `firefox`). |
| `CookiesBrowserProfile` | Perfil do navegador passado ao `yt-dlp`; o padrão é `Default`. |
| `CookiesFile` | Caminho do arquivo de cookies; o padrão é `lib\cookies.txt`. |

Se a configuração estiver ausente ou não puder ser lida como JSON válido, o aplicativo inicia com valores padrão. As alterações persistidas são gravadas no arquivo ao lado do executável; garanta permissão de escrita nessa pasta.

## Estrutura do repositório

```text
VideoDownloader.slnx
VideoDownloaderDesktop/
├── Program.cs                    # Inicialização da aplicação
├── DownloaderForm.cs             # Interface, fila e eventos de download
├── DownloaderForm.Designer.cs    # Layout dos controles Windows Forms
├── DownloadService.cs            # Validação, criação da fila e políticas por site
├── DownloadSitePolicy.cs         # Comportamento padrão, YouTube e Instagram
├── YtDlpClient.cs                # Execução do yt-dlp e seleção de formato
├── YtDlpLocator.cs               # Localização do executável yt-dlp
├── YtDlpOptions.cs               # Opções de execução
├── CookieProvider.cs             # Escolha entre arquivo e navegador
├── CookieFileHelper.cs            # Detecção de sites, URLs e cookies
├── BrowserCookieHelper.cs         # Diagnósticos de navegador/autenticação
├── DownloadErrorTranslator.cs     # Tradução de falhas conhecidas
├── DownloadMessages.cs            # Mensagens da aplicação
├── DownloadRequest.cs              # Modelo da solicitação e validação
├── DownloadQueueItem.cs            # Modelo e estado de cada item da fila
├── DownloadAttemptResult.cs        # Resultado de uma tentativa
├── AppSettingsData.cs              # Modelo de configuração
├── AppSettingsRepository.cs        # Leitura e gravação das configurações
├── appsettings.json                # Valores iniciais de configuração
└── lib/
    ├── yt-dlp.exe                  # Extrator de mídia empacotado
    └── cookies.txt                 # Cookies locais (sensíveis)
```

## Diagnóstico rápido

- **`yt-dlp não encontrado`**: confirme que `lib\yt-dlp.exe` está ao lado do executável, dentro da pasta `lib`.
- **Erro ao copiar banco de cookies**: feche o navegador e seus processos em segundo plano e tente novamente, ou use um arquivo de cookies.
- **`Failed to decrypt with DPAPI`**: selecione Firefox ou use cookies exportados para `lib\cookies.txt`.
- **Instagram exige login / resposta de mídia vazia**: atualize os cookies enquanto estiver conectado ao Instagram e confirme que o link é acessível pela conta.
- **Arquivo existente**: confirme a substituição no diálogo ou escolha outro nome base/pasta.
- **Resolução solicitada indisponível**: o log informa a resolução alternativa selecionada.
- **Falha ao combinar áudio e vídeo**: instale o FFmpeg e certifique-se de que o `yt-dlp` consiga encontrá-lo no ambiente.

## Tecnologias

- C# e .NET 10 para Windows.
- Windows Forms.
- `yt-dlp` para extração e download de mídia.

