function showChart(container, posData,dateData) {
    var chartCanvas = container.parentElement.querySelector('canvas');
    chartCanvas.style.display = 'block';

    // Устанавливаем размеры canvas согласно родительскому контейнеру
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
            labels: dateData,
            datasets: [{ label: '', data: posData, fill: true, borderColor: 'rgb(75, 192, 192)', tension: 0.0 }]
        },
        options: {
            responsive: false,
            maintainAspectRatio: false,
            animations: {
                duration: 0.01
            },
            scales: {
                y: {
                    min: minValue, // Инвертирование: минимальное значение устанавливаем как максимальное
                    max: maxValue,
                    reverse: true  // Инвертирование: максимальное значение устанавливаем как минимальное
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