# Jellyfin NotificationCenter

Smart notifications for new media with intelligent filtering based on watch history. Messages are localized using Jellyfin's server language setting.

## Features

- Notification bell with badge in Jellyfin header
- 3 relevance levels: All / Relevant / Highly Relevant
- Per media type config: Movies, Series, Music
- i18n: notifications follow server locale (118+ languages)

## Requirements

- Jellyfin **10.11.0+**
- [File Transformation](https://github.com/IAmParadox27/jellyfin-plugin-file-transformation) plugin (injects the notification bell into the UI)

## Installation

Add both repositories in **Dashboard > Plugins > Repositories**:

| Name | URL |
|------|-----|
| File Transformation | `https://www.iamparadox.dev/jellyfin/plugins/manifest.json` |
| NotificationCenter | `https://raw.githubusercontent.com/gravity-zero/NotificationCenter/master/manifest.json` |

Install **File Transformation** and **NotificationCenter** from the catalog, then restart Jellyfin once.

## Configuration

**Dashboard > Plugins > NotificationCenter > Parameters**

| Level | Behavior |
|-------|----------|
| All | Every new media addition |
| Relevant | Based on watch history |
| Highly Relevant | Currently watching or strong genre matches |

## License

MIT
