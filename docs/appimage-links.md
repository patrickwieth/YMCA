# Linux AppImage browser / Discord links

The AppImage supports `ymca://` join/replay links. The launcher now offers a
one-time, user-local registration when a graphical session and zenity or kdialog
are available and there is no existing handler. Declining is remembered. An
existing handler is not silently replaced. No root privileges are needed.

Explicit registration (also works without the graphical prompt):

```sh
./YMCA-<version>-x86_64.AppImage --register-links
```

This registers links and exits without starting another game. `xdg-utils` is
required; failure of optional desktop integration never prevents a normal launch.
A previously approved launcher is refreshed when the AppImage is moved/updated
and started again, but not after the user chooses another default application.

Owned files are under `$XDG_DATA_HOME/applications/ymca-appimage.desktop`,
`$XDG_DATA_HOME/ymca-appimage/launch`, and `$XDG_CONFIG_HOME/ymca/link-registration`
(with the usual ~/.local/share and ~/.config defaults). The tiny launcher quotes
the AppImage path as shell data and forwards arguments unchanged. Desktop Exec
uses a fixed /bin/sh executable and a separate %u argument; this also avoids GIO's
executable-lookup issue with literal percent characters in AppImage filenames.
No URI is evaluated as shell code. Control characters in the image path are rejected.

Implementation: `packaging/linux/register-links.sh`, copied into the AppImage by
the package builder; `appimage-link-registration.patch` hooks its optional prompt
and explicit registration into the existing launcher. Normal URI parsing remains
in the existing cross-platform launch handler.

`tools/test_appimage_links.py` checks consent, repeat starts, moving the image,
existing and subsequently changed defaults, headless starts and argument quoting.
Native Linux CI additionally launches a URI through GIO with a filename containing
spaces, quotes, backslashes, dollar/backtick characters and a percent sign. A
separate native test uses real xdg-mime with isolated HOME/XDG directories; no
real desktop association or tournament server is touched.
