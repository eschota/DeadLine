function showChart(container, posData) {
    var chartCanvas = container.parentElement.querySelector('canvas');
    chartCanvas.style.display = 'block';

    // Ð£ÑÑ‚Ð°Ð½Ð°Ð²Ð»Ð¸Ð²Ð°ÐµÐ¼ Ñ€Ð°Ð·Ð¼ÐµÑ€Ñ‹ canvas ÑÐ¾Ð³Ð»Ð°ÑÐ½Ð¾ Ñ€Ð¾Ð´Ð¸Ñ‚ÐµÐ»ÑŒÑÐºÐ¾Ð¼Ñƒ ÐºÐ¾Ð½Ñ‚ÐµÐ¹Ð½ÐµÑ€Ñƒ
    chartCanvas.width = container.offsetWidth;
    chartCanvas.height = container.offsetHeight;

    if (chartCanvas.chart) {
        chartCanvas.chart.destroy();
    }

    var minValue = Math.min(...posData);
    var maxValue = Math.max(...posData);

    var ctx = chartCanvas.getContext('2d');
    chartCanvas.chart = new Chart(ctx, {
        type: 'line',
        data: {
            labels: posData.map((_, index) => ` ${index + 1}`),
            datasets: [{ label: 'Pos', data: posData, fill: true, borderColor: 'rgb(75, 192, 192)', tension: 0.0 }]
        },
        options: {
            responsive: false,
            maintainAspectRatio: false,
            animations: {
                duration: 0
            },
            scales: {
                y: {
                    min: minValue, // Ð˜Ð½Ð²ÐµÑ€Ñ‚Ð¸Ñ€Ð¾Ð²Ð°Ð½Ð¸Ðµ: Ð¼Ð¸Ð½Ð¸Ð¼Ð°Ð»ÑŒÐ½Ð¾Ðµ Ð·Ð½Ð°Ñ‡ÐµÐ½Ð¸Ðµ ÑƒÑÑ‚Ð°Ð½Ð°Ð²Ð»Ð¸Ð²Ð°ÐµÐ¼ ÐºÐ°Ðº Ð¼Ð°ÐºÑÐ¸Ð¼Ð°Ð»ÑŒÐ½Ð¾Ðµ
                    max: maxValue,  // Ð˜Ð½Ð²ÐµÑ€Ñ‚Ð¸Ñ€Ð¾Ð²Ð°Ð½Ð¸Ðµ: Ð¼Ð°ÐºÑÐ¸Ð¼Ð°Ð»ÑŒÐ½Ð¾Ðµ Ð·Ð½Ð°Ñ‡ÐµÐ½Ð¸Ðµ ÑƒÑÑ‚Ð°Ð½Ð°Ð²Ð»Ð¸Ð²Ð°ÐµÐ¼ ÐºÐ°Ðº Ð¼Ð¸Ð½Ð¸Ð¼Ð°Ð»ÑŒÐ½Ð¾Ðµ
                    reverse: true
                }
            },
            plugins: {
                legend: {
                    display: false
                }
            }
        }
    });
}

function hideChart(container) {
    var chartCanvas = container.parentElement.querySelector('canvas');
    if (chartCanvas) {
        chartCanvas.style.display = 'none';
    }
}