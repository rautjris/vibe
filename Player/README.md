# MySpeaker

MySpeaker is a Blazor Server dashboard for managing an Arylic-based network speaker through the HTTP API. It surfaces live playback status, provides quick access to playback and input controls, and lets you curate a personal list of stream URLs that are stored on the server.

## Features
- Live status card that decodes track metadata and shows transport, volume, loop, and playlist information.
- Transport, volume, input source, and loop controls wired to the speaker HTTP endpoints.
- Quick play box for ad-hoc stream URLs and a library of saved streams stored in `App_Data/streams.json`.
- Stylish dark UI with responsive layout for desktops and tablets.

## Configuration
1. Update `Speaker:BaseUrl` inside `appsettings.Development.json` (and `appsettings.json` for production) so it points to your speaker, e.g. `http://192.168.1.42`.
2. Toggle `Speaker:UseMock` to `true` when you want to run the dashboard without a real device. The development profile enables the mock by default.
3. Adjust `Speaker:RequestTimeoutSeconds` if the device needs more time to respond.
4. The default stream library lives in `App_Data/streams.json`. The application keeps this file updated; you can seed it with starter stations if desired.

## Running the app
```powershell
# Restore and launch the dashboard
cd c:\Project\temp\Player
dotnet run --project MySpeaker.csproj
```

Then browse to the URL printed by `dotnet run` (typically `https://localhost:5001`).

## Updating the HTTP endpoints
The speaker commands are encapsulated in `Services/SpeakerApiClient.cs`. Extend this class if you need additional endpoints from the Arylic HTTP API. UI interactions are handled in `Components/Pages/Home.razor`.
