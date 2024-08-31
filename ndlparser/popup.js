document.addEventListener('DOMContentLoaded', function () {
  // Загрузка сохранённых настроек
  chrome.storage.sync.get(['nickname', 'activeMode'], function (data) {
    if (data.nickname) {
      document.getElementById('nickname').value = data.nickname;
    }
    document.getElementById('mode').checked = data.activeMode || false;
  });

  // Сохранение настроек
  document.getElementById('saveButton').addEventListener('click', function () {
    const nickname = document.getElementById('nickname').value;
    const activeMode = document.getElementById('mode').checked;

    chrome.storage.sync.set({ nickname, activeMode }, function () {
      console.log('Settings saved:', { nickname, activeMode });
    });
  });
});
