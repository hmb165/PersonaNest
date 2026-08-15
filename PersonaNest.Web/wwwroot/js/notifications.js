// PersonaNest — real-time notification delivery (Phase 15, SignalR). Progressive enhancement
// only: the bell and /Notifications page already work from a plain page load without this file;
// it just makes new notifications appear live instead of waiting for the next navigation (§12).
(function () {
    'use strict';

    if (typeof signalR === 'undefined') {
        return;
    }

    var badge = document.querySelector('[data-notification-count]');
    var list = document.querySelector('[data-notification-list]');
    var empty = document.querySelector('[data-notification-empty]');

    function bumpBadge() {
        if (!badge) {
            return;
        }
        var current = parseInt(badge.textContent, 10) || 0;
        badge.textContent = current + 1;
        badge.classList.remove('d-none');
    }

    function prependToList(notification) {
        if (!list) {
            return;
        }
        if (empty) {
            empty.remove();
            empty = null;
        }

        var item = document.createElement('a');
        item.className = 'notification-bell-item notification-bell-item-unread';
        item.href = notification.url || '#';

        var message = document.createElement('span');
        message.className = 'notification-bell-item-message';
        message.textContent = notification.message;

        var time = document.createElement('span');
        time.className = 'notification-bell-item-time';
        time.textContent = 'Just now';

        item.appendChild(message);
        item.appendChild(time);
        list.insertBefore(item, list.firstChild);

        while (list.children.length > 5) {
            list.removeChild(list.lastChild);
        }
    }

    function showToast(notification) {
        var toast = document.createElement('div');
        toast.className = 'alert alert-info notification-toast';
        toast.setAttribute('role', 'status');
        toast.innerHTML = '<span class="alert-icon">&#128276;</span><div class="alert-body"></div>';
        toast.querySelector('.alert-body').textContent = notification.message;

        document.body.appendChild(toast);

        window.setTimeout(function () {
            toast.classList.add('notification-toast-out');
            window.setTimeout(function () { toast.remove(); }, 300);
        }, 5000);
    }

    var connection = new signalR.HubConnectionBuilder()
        .withUrl('/hubs/notifications')
        .withAutomaticReconnect()
        .build();

    connection.on('ReceiveNotification', function (notification) {
        bumpBadge();
        prependToList(notification);
        showToast(notification);
    });

    connection.start().catch(function (err) {
        console.error('Notification hub connection failed:', err);
    });
})();
