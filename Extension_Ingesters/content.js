

(async () => {
  
    // Ждем выполнения всех скриптов на странице
    await new Promise(resolve => {
      if (document.readyState === 'complete') {
        resolve();
      } else {
        window.addEventListener('load', resolve);
      }
    });
  
    // Некоторая задержка для завершения всех асинхронных операций на странице
    await new Promise(resolve => setTimeout(resolve, 2000));
  
    // Получаем HTML страницы
    const htmlContent = document.documentElement.outerHTML;
  
    // Отправляем HTML в фоновый скрипт

    
    
    if (!chrome.runtime) {
      debugger;

    }

    if (chrome.runtime) {
      chrome.runtime.sendMessage({ htmlContent: htmlContent });
    }
    console.assert(chrome.runtime);
  })();