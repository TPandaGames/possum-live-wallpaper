# Third-party notices

Pocket Possum bundles or depends on the following. Each remains under its own license.

| Component | Used for | License |
|---|---|---|
| [Fredoka](https://fonts.google.com/specimen/Fredoka) (via [@fontsource/fredoka](https://fontsource.org/fonts/fredoka)) | UI font, bundled in `wwwroot/fonts/` | SIL Open Font License 1.1 |
| [JetBrains Mono](https://www.jetbrains.com/lp/mono/) (via [@fontsource/jetbrains-mono](https://fontsource.org/fonts/jetbrains-mono)) | Clock font, bundled in `wwwroot/fonts/` | SIL Open Font License 1.1 |
| [Tailwind CSS](https://tailwindcss.com/) 3.4 | Compiled into `wwwroot/app.css` at development time only | MIT |
| [Microsoft.Web.WebView2](https://www.nuget.org/packages/Microsoft.Web.WebView2) | Hosts the page in the screensaver window | Microsoft software license terms (see the NuGet package) |

### Weather data

When the optional weather sync is enabled, current conditions, sunrise/sunset and city search results come from
[Open-Meteo.com](https://open-meteo.com/) (forecast and geocoding APIs). Open-Meteo's data is licensed
[CC BY 4.0](https://creativecommons.org/licenses/by/4.0/) and its free API is for non-commercial use.

The WebView2 Runtime and the .NET Desktop Runtime are installed separately by the user.

Emoji are rendered by the operating system's emoji font and are not bundled.
