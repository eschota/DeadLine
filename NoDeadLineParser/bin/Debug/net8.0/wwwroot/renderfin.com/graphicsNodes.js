function showChart(container, dateData, cpuData, gpuData, taskData) {
    var chartCanvas = container.parentElement.querySelector('canvas');
    chartCanvas.style.display = 'block';

    // Ð£ÑÑ‚Ð°Ð½Ð°Ð²Ð»Ð¸Ð²Ð°ÐµÐ¼ Ñ€Ð°Ð·Ð¼ÐµÑ€Ñ‹ canvas ÑÐ¾Ð³Ð»Ð°ÑÐ½Ð¾ Ñ€Ð¾Ð´Ð¸Ñ‚ÐµÐ»ÑŒÑÐºÐ¾Ð¼Ñƒ ÐºÐ¾Ð½Ñ‚ÐµÐ¹Ð½ÐµÑ€Ñƒ
    chartCanvas.width = container.offsetWidth;
    chartCanvas.height = container.offsetHeight;

    if (chartCanvas.chart) {
        chartCanvas.chart.destroy();
    }

    var minValue = Math.min(...cpuData, ...gpuData);
    var maxValue = Math.max(...cpuData, ...gpuData);

    var ctx = chartCanvas.getContext('2d');
    chartCanvas.chart = new Chart(ctx, {
        type: 'line',
        data: {
            labels: dateData,
            datasets: [
                { label: 'CPU', data: cpuData, borderColor: 'rgb(75, 192, 192)', tension: 0.0, fill: false },
                { label: 'GPU', data: gpuData, borderColor: 'rgb(192, 75, 192)', tension: 0.0, fill: false },
                { label: 'Tasks', data: taskData, borderColor: 'rgb(192, 192, 75)', tension: 0.0, fill: false } // ÐÐ¾Ð²Ñ‹Ð¹ Ð´Ð°Ñ‚Ð°ÑÐµÑ‚ Ð´Ð»Ñ Ð¾Ñ‚Ð¾Ð±Ñ€Ð°Ð¶ÐµÐ½Ð¸Ñ Ð²Ñ‹Ð¿Ð¾Ð»Ð½ÐµÐ½Ð½Ñ‹Ñ… Ð·Ð°Ð´Ð°Ñ‡
            ]
        },
        options: {
            responsive: false,
            maintainAspectRatio: false,
            scales: {
                y: {
                    min: minValue,
                    max: 100, // Ð˜Ð·Ð¼ÐµÐ½ÐµÐ½Ð¾ Ð´Ð»Ñ ÐºÐ¾Ñ€Ñ€ÐµÐºÑ‚Ð½Ð¾Ð³Ð¾ Ð¾Ñ‚Ð¾Ð±Ñ€Ð°Ð¶ÐµÐ½Ð¸Ñ Ð¼Ð°ÐºÑÐ¸Ð¼Ð°Ð»ÑŒÐ½Ð¾Ð³Ð¾ Ð·Ð½Ð°Ñ‡ÐµÐ½Ð¸Ñ Ñ ÑƒÑ‡ÐµÑ‚Ð¾Ð¼ taskData
                    ticks: {
                        display: true // Ð’ÐºÐ»ÑŽÑ‡Ð¸Ñ‚ÑŒ Ð¾Ñ‚Ð¾Ð±Ñ€Ð°Ð¶ÐµÐ½Ð¸Ðµ Ð¿Ð¾Ð´Ð¿Ð¸ÑÐµÐ¹ Ð½Ð° Ð¾ÑÐ¸ Y
                    }
                },
                x: {
                    ticks: {
                        display: true // Ð˜Ð·Ð¼ÐµÐ½ÐµÐ½Ð¾ Ð´Ð»Ñ Ð²ÐºÐ»ÑŽÑ‡ÐµÐ½Ð¸Ñ Ð¾Ñ‚Ð¾Ð±Ñ€Ð°Ð¶ÐµÐ½Ð¸Ñ Ð¿Ð¾Ð´Ð¿Ð¸ÑÐµÐ¹ Ð½Ð° Ð¾ÑÐ¸ X
                    }
                }
            },
            plugins: {
                legend: {
                    display: true // Ð’ÐºÐ»ÑŽÑ‡Ð¸Ñ‚ÑŒ Ð»ÐµÐ³ÐµÐ½Ð´Ñƒ Ð´Ð»Ñ Ð¾Ñ‚Ð¾Ð±Ñ€Ð°Ð¶ÐµÐ½Ð¸Ñ Ð½Ð°Ð·Ð²Ð°Ð½Ð¸Ð¹ Ð³Ñ€Ð°Ñ„Ð¸ÐºÐ¾Ð²
                }
            }
        }
    });
}
