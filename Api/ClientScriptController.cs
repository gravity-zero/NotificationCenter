using System.Net.Mime;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Plugin.NotificationCenter.Api;

/// <summary>
/// Serves the notification client JavaScript.
/// </summary>
[ApiController]
[Route("NotificationCenter")]
[AllowAnonymous]
public class ClientScriptController : ControllerBase
{
    private const string ClientScript = @"
(function() {
    'use strict';

    const CONFIG = {
        pollInterval: 30000,
        apiEndpoint: '/NotificationCenter',
        maxNotificationsDisplay: 10
    };

    let notificationBell = null;
    let notificationPanel = null;
    let unreadCount = 0;

    function getApiClient() {
        return window.ApiClient || (window.require ? window.require(['ApiClient']) : null);
    }

    function createNotificationBell() {
        const bell = document.createElement('button');
        bell.id = 'notification-bell';
        bell.className = 'paper-icon-button-light';
        bell.innerHTML = `
            <span class='material-icons notification-icon'>notifications</span>
            <span class='notification-badge' style='display: none;'>0</span>
        `;
        bell.style.cssText = 'position: relative; margin: 0 0.5em; overflow: visible !important;';
        bell.setAttribute('title', 'Notifications');
        
        const badge = bell.querySelector('.notification-badge');
        badge.style.cssText = `
            position: absolute;
            top: -4px;
            right: -4px;
            background: #e53935;
            color: white;
            border-radius: 50%;
            font-size: 10px;
            font-weight: bold;
            width: 20px;
            height: 20px;
            display: flex;
            align-items: center;
            justify-content: center;
            line-height: 1;
        `;

        bell.addEventListener('click', toggleNotificationPanel);
        return bell;
    }

    function createNotificationPanel() {
        const panel = document.createElement('div');
        panel.id = 'notification-panel';
        panel.style.cssText = `
            position: fixed;
            top: 60px;
            right: 20px;
            width: 400px;
            max-height: 600px;
            background: #1c1c1c;
            border: 1px solid #333;
            border-radius: 8px;
            box-shadow: 0 4px 20px rgba(0,0,0,0.5);
            z-index: 10000;
            display: none;
            overflow: hidden;
        `;

        panel.innerHTML = `
            <div style='padding: 16px; border-bottom: 1px solid #333; display: flex; justify-content: space-between; align-items: center;'>
                <h3 style='margin: 0; font-size: 18px;'>Notifications</h3>
                <button id='mark-all-read' class='paper-icon-button-light' title='Mark all as read' style='opacity: 0.7;'>
                    <span class='material-icons' style='font-size: 20px;'>done_all</span>
                </button>
            </div>
            <div id='notification-list' style='max-height: 500px; overflow-y: auto; padding: 8px;'>
                <div style='text-align: center; padding: 20px; color: #999;'>Loading...</div>
            </div>
        `;

        panel.querySelector('#mark-all-read').addEventListener('click', markAllAsRead);
        return panel;
    }

    async function fetchNotifications(unreadOnly = false) {
        try {
            const apiClient = getApiClient();
            if (!apiClient) return [];

            return await apiClient.getJSON(`${CONFIG.apiEndpoint}?unreadOnly=${unreadOnly}`);
        } catch (error) {
            console.error('Error fetching notifications:', error);
            return [];
        }
    }

    async function fetchUnreadCount() {
        try {
            const apiClient = getApiClient();
            if (!apiClient) return 0;

            return await apiClient.getJSON(`${CONFIG.apiEndpoint}/unread/count`);
        } catch (error) {
            console.error('Error fetching unread count:', error);
            return 0;
        }
    }

    async function markAsRead(notificationId) {
        try {
            const apiClient = getApiClient();
            if (!apiClient) return;

            await apiClient.ajax({
                type: 'POST',
                url: `${CONFIG.apiEndpoint}/${notificationId}/read`
            });

            updateUnreadCount();
        } catch (error) {
            console.error('Error marking notification as read:', error);
        }
    }

    async function markAllAsRead() {
        const notifications = await fetchNotifications(true);
        for (const notif of notifications) {
            await markAsRead(notif.Id);
        }
        await loadNotifications();
    }

    async function updateUnreadCount() {
        const count = await fetchUnreadCount();
        unreadCount = count;

        const badge = notificationBell?.querySelector('.notification-badge');
        if (badge) {
            badge.textContent = count;
            badge.style.display = count > 0 ? 'flex' : 'none';
        }
    }

    function navigateToItem(itemId) {
        const apiClient = getApiClient();
        if (!apiClient || !itemId) return;

        window.location.href = `#!/details?id=${itemId}`;
    }

    function renderNotifications(notifications) {
        const listContainer = document.getElementById('notification-list');
        if (!listContainer) return;

        if (notifications.length === 0) {
            listContainer.innerHTML = ""<div style='text-align: center; padding: 20px; color: #999;'>No notifications</div>"";
            return;
        }

        listContainer.innerHTML = notifications
            .slice(0, CONFIG.maxNotificationsDisplay)
            .map(notif => {
                const date = new Date(notif.CreatedAt);
                const isUnread = !notif.ReadAt;
                
                return `
                    <div class='notification-item' data-id='${notif.Id}' data-item-id='${notif.ItemId || ''}' style='
                        padding: 12px;
                        margin: 8px 0;
                        background: ${isUnread ? '#2a2a2a' : '#1c1c1c'};
                        border-left: 3px solid ${isUnread ? '#00a4dc' : 'transparent'};
                        border-radius: 4px;
                        cursor: pointer;
                        transition: background 0.2s;
                    ' onmouseover=""this.style.background='#333'"" onmouseout=""this.style.background='${isUnread ? '#2a2a2a' : '#1c1c1c'}'"">
                        <div style='font-weight: ${isUnread ? 'bold' : 'normal'}; margin-bottom: 4px;'>
                            ${notif.Title}
                        </div>
                        <div style='font-size: 13px; color: #aaa; margin-bottom: 4px;'>
                            ${notif.Message}
                        </div>
                        <div style='font-size: 11px; color: #666;'>
                            ${date.toLocaleString()}
                        </div>
                    </div>
                `;
            })
            .join('');

        listContainer.querySelectorAll('.notification-item').forEach(item => {
            item.addEventListener('click', async () => {
                const notifId = item.dataset.id;
                const itemId = item.dataset.itemId;
                
                await markAsRead(notifId);
                closeNotificationPanel();
                
                if (itemId && itemId !== 'null' && itemId !== '') {
                    navigateToItem(itemId);
                }
            });
        });
    }

    function toggleNotificationPanel() {
        if (notificationPanel.style.display === 'none') {
            openNotificationPanel();
        } else {
            closeNotificationPanel();
        }
    }

    async function openNotificationPanel() {
        notificationPanel.style.display = 'block';
        await loadNotifications();
    }

    function closeNotificationPanel() {
        notificationPanel.style.display = 'none';
    }

    async function loadNotifications() {
        const notifications = await fetchNotifications(false);
        renderNotifications(notifications);
    }

    function init() {
        const checkInterval = setInterval(() => {
            const headerRight = document.querySelector('.headerRight');
            if (headerRight && !document.getElementById('notification-bell')) {
                clearInterval(checkInterval);

                notificationBell = createNotificationBell();
                headerRight.insertBefore(notificationBell, headerRight.firstChild);

                notificationPanel = createNotificationPanel();
                document.body.appendChild(notificationPanel);

                document.addEventListener('click', async (e) => {
                    if (notificationPanel.style.display === 'block' && 
                        !notificationPanel.contains(e.target) && 
                        !notificationBell.contains(e.target)) {
                        
                        const unreadNotifications = await fetchNotifications(true);
                        for (const notif of unreadNotifications) {
                            await markAsRead(notif.Id);
                        }
                        
                        closeNotificationPanel();
                        await updateUnreadCount();
                    }
                });

                updateUnreadCount();
                setInterval(updateUnreadCount, CONFIG.pollInterval);

                console.log('NotificationCenter initialized');
            }
        }, 500);

        setTimeout(() => clearInterval(checkInterval), 30000);
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }
})();
";

    /// <summary>
    /// Serves the notification client JavaScript.
    /// </summary>
    /// <returns>JavaScript file.</returns>
    [HttpGet("client.js")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult GetClientScript()
    {
        return Content(ClientScript, MediaTypeNames.Text.JavaScript, Encoding.UTF8);
    }
}
