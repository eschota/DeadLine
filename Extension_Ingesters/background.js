async function sendToServer(serverUrl, requestBody) {
  try {
    const serverResponse = await fetch(serverUrl, {
      method: 'POST',
      headers: {
        'Content-Type': 'text/html',
      },
      body: requestBody,
    });

    const serverResponseText = await serverResponse.text();
    console.log('Server response:', serverResponseText);
  } catch (error) {
    console.error('Error in sendToServer:', error);
  }
}

chrome.runtime.onMessage.addListener((message, sender, sendResponse) => {
  if (message.htmlContent) {
    const pageUrl = sender.tab.url;
    const serverUrl = 'https://renderfin.com/adminapprove';
    const requestBody = pageUrl + '<!--URL-->' + message.htmlContent;
    sendToServer(serverUrl, requestBody).then(() => {
      // Проверяем, существует ли вкладка перед её закрытием
      chrome.tabs.remove(sender.tab.id, () => {
        if (chrome.runtime.lastError) {
          // console.error(chrome.runtime.lastError.message);
        }
      });
    }).catch(() => {
      chrome.tabs.remove(sender.tab.id, () => {
        if (chrome.runtime.lastError) {
          console.error(chrome.runtime.lastError.message);
        }
      });
    });
  }
});

function checkPageAndSendApprove() {
  const targetUrl = 'https://qwertystock.com/adminapprove';

  chrome.tabs.create({ url: targetUrl, active: false }, (tab) => {
    chrome.tabs.onUpdated.addListener(function listener(tabId, info) {
      if (info.status === 'complete' && tabId === tab.id) {
        chrome.tabs.onUpdated.removeListener(listener);
        chrome.scripting.executeScript({
          target: { tabId: tab.id },
          files: ['content.js']
        }).then(() => {
          if (chrome.runtime.lastError) {
            console.error(chrome.runtime.lastError.message);
          }
          chrome.tabs.remove(tab.id, () => {
            if (chrome.runtime.lastError) {
              console.error(chrome.runtime.lastError.message);
            }
          });
        }).catch((error) => {
          console.error('Error executing script:', error);
          chrome.tabs.remove(tab.id, () => {
            if (chrome.runtime.lastError) {
              console.error(chrome.runtime.lastError.message);
            }
          });
        });
      }
    });
  });
}

function checkPageAndSend() {


  try {
  const pageUrl = 'https://accounts.stocksubmitter.com/cp/ingestionqueue';
  const serverUrl = 'https://renderfin.com/ingesters';

  fetchPageContent(pageUrl).then(htmlContent => {
    if (htmlContent) {
      const requestBody = pageUrl + '<!--URL-->' + htmlContent;
      sendToServer(serverUrl, requestBody);
    }
  });
    } catch (error) {
      console.error('Error ChekPageAndSend page content:', error);
      return null;
    }
}

async function fetchPageContent(pageUrl) {
  try {
    const response = await fetch(pageUrl);
    if (!response.ok) throw new Error('Network response was not ok');
    const html = await response.text();
    return html;
  } catch (error) {
    console.error('Error fetching page content:', error);
    return null;
  }
}

async function main() {
  setInterval(checkPageAndSend, 60000);
  setInterval(checkPageAndSendApprove, 160000);

  // Запускаем проверку сразу при загрузке расширения
  checkPageAndSend();
  checkPageAndSendApprove();
}

main().catch(console.error);