## Intrduction
_NDeye_ is a small .NET executable that is able to capture frames from an NDI&trade; source on the Network.
Additionally, NDeye can find and decode QR codes on the captured frames.
Both, the image and any found qr contents will be provided to the user as HTTP entpoints.
## Endpoints
### /snapshot
Returns the image as PNG.
With an optional query parameter `?download=true`, a direct download of the image can be requested
### /qr
All found QR code contents are displayed (links are clickable).
With an additional query parameter `?redirect=true`, the user agent is redirected to the first found link if any

## Configuration
The NDI&trade; source name can be passed in appsettings.json, or alternatively as environment variable `Ndi__SourceName`.
Make sure to give a source name like: `MachineName (SourceName)`. (This is an NDI&trade; convention.)