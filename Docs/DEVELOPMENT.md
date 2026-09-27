# Development Notes

## Reference

Anka2 is used as a technical reference/foundation where its GPL-3.0 terms permit. Its repository is:

https://github.com/ybeststudio/Anka2Project

The specific fork inspected during setup is:

https://github.com/brunao97/metin2-project

## Important

Do not commit proprietary game assets, leaked client files, credentials, server keys or unrelated third-party binaries.

Keep imported GPL code and its notices intact.

## Build strategy

The first practical target is Windows for the client and a separate server environment for the server stack. The reference project documents Visual Studio 2022 for the client and FreeBSD for the server.

Before changing the rendering/network core, get the reference build working. Then modify one subsystem at a time and commit each working milestone.
