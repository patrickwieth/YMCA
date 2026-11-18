#!/bin/sh
# User-local, opt-in registration. Never evaluates a URI or requires root.
set -eu
image=$(readlink -f "${1:?AppImage path required}")
mode=${2:---prompt}
[ -f "$image" ] && [ -x "$image" ] || exit 1
clean=$(printf '%s' "$image" | LC_ALL=C tr -d '\000-\037\177')
[ "$image" = "$clean" ] || { echo 'Cannot register an AppImage path containing control characters.' >&2; exit 1; }
case "$mode" in --prompt|--register) ;; *) exit 1 ;; esac
if ! command -v xdg-mime >/dev/null 2>&1; then
    [ "$mode" = --prompt ] && exit 0
    echo 'Please install xdg-utils to register YMCA links.' >&2
    exit 1
fi
data=${XDG_DATA_HOME:-"$HOME/.local/share"}
config=${XDG_CONFIG_HOME:-"$HOME/.config"}/ymca
name=ymca-appimage.desktop
entry="$data/applications/$name"
choice="$config/link-registration"
current=$(xdg-mime query default x-scheme-handler/ymca 2>/dev/null || :)
previous=$(test ! -f "$choice" || head -n 1 "$choice")
if [ "$mode" = --prompt ]; then
    if [ "$previous" = accepted ]; then
        # Refresh a previously approved launcher after moving/updating the image,
        # but never undo a user's subsequent change to another default handler.
        [ "$current" = "$name" ] || exit 0
        grep -q '^X-YMCA-AppImage-Registration=true$' "$entry" 2>/dev/null || exit 0
    elif [ -n "$previous" ] || [ -n "$current" ]; then
        exit 0
    else
        if [ -z "${DISPLAY:-}${WAYLAND_DISPLAY:-}" ]; then exit 0; fi
        answer=declined
        message='Allow this YMCA AppImage to open ymca:// links from Discord and your browser? You can enable this later with --register-links.'
        if command -v zenity >/dev/null 2>&1; then
            if zenity --question --default-cancel --title='YMCA links' --text="$message"; then answer=accepted; fi
        elif command -v kdialog >/dev/null 2>&1; then
            if kdialog --title 'YMCA links' --defaultno --yesno "$message"; then answer=accepted; fi
        else
            echo 'To enable browser/Discord links, run this AppImage with --register-links.' >&2
            exit 0
        fi
        mkdir -p "$config"
        if [ "$answer" != accepted ]; then printf 'declined\n' > "$choice"; exit 0; fi
    fi
fi
mkdir -p "$data/applications" "$config"
# Two escaping layers: the Exec quoted argument, then the desktop-entry string.
# %% is a literal percent in an Exec field. The URI remains its own %u argument.
escaped=$(printf '%s' "$image" | sed 's/\\/\\\\/g; s/["`$]/\\&/g; s/%/%%/g' | sed 's/\\/\\\\/g')
temporary=$(mktemp "$data/applications/.ymca-links.XXXXXX")
trap 'rm -f "$temporary"' 0 HUP INT TERM
printf '[Desktop Entry]\nType=Application\nName=YMCA\nExec="%s" %%u\nIcon=applications-games\nTerminal=false\nCategories=Game;StrategyGame;\nMimeType=x-scheme-handler/ymca;\nX-YMCA-AppImage-Registration=true\n' "$escaped" > "$temporary"
chmod 644 "$temporary"
mv "$temporary" "$entry"
if command -v update-desktop-database >/dev/null 2>&1; then update-desktop-database "$data/applications" || :; fi
xdg-mime default "$name" x-scheme-handler/ymca
[ "$(xdg-mime query default x-scheme-handler/ymca)" = "$name" ] || { echo 'The desktop did not accept the YMCA link handler.' >&2; exit 1; }
printf 'accepted\n' > "$choice"
