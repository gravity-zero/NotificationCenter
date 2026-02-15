# Jellyfin NotificationCenter

Smart notifications for new media with intelligent filtering based on watch history. Messages are localized using Jellyfin's server language setting.

## Features

- Notification bell with badge in Jellyfin header
- 3 relevance levels: All / Relevant / Highly Relevant
- Per media type config: Movies, Series, Music
- i18n: notifications follow server locale (118+ languages)

## Installation

Add repository in Jellyfin Dashboard > Plugins > Repositories:
```
https://raw.githubusercontent.com/gravity-zero/NotificationCenter/master/manifest.json
```

## Configuration

**Dashboard > Plugins > NotificationCenter > Parameters**

| Level | Behavior |
|-------|----------|
| All | Every new media addition |
| Relevant | Based on watch history |
| Highly Relevant | Currently watching or strong genre matches |

## License

MIT
