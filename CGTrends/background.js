// Здесь указываем URL сервера, на который будет отправлен HTML
const serverUrl = 'https://qwertystock.com';

fetch(`${serverUrl}/tunnel-for-trends`, {
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