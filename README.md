# Jellyfin NotificationCenter

Smart notifications for new media with intelligent filtering based on watch history.

## Features

- 🔔 Notification bell with badge in Jellyfin header
- 🎯 3 relevance levels: All / Relevant / Highly Relevant  
- 🎬 Configure per media type: Movies, Series, Music, Books

## Installation

Add repository in Jellyfin:
```
https://raw.githubusercontent.com/YOUR_USERNAME/jellyfin-plugin-notificationcenter/main/manifest.json
```

## Configuration (For all Users)

**Dashboard → Plugins → NotificationCenter → Parameters**

- **Level 1 (All)**: Every new media addition
- **Level 2 (Relevant)**: Based on watch history (movies/series/genres)
- **Level 3 (Highly Relevant)**: Currently watching or strong matches

## License

MIT