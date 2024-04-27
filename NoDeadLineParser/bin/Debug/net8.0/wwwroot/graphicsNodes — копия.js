function showChart(container, cpuData, gpuData, dateData) {
    var chartCanvas = container.parentElement.querySelector('canvas');
    chartCanvas.style.display = 'block';

    // Устанавливаем размеры canvas согласно родительскому контейнеру
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
                { label: 'GPU', data: gpuData, borderColor: 'rgb(192, 75, 192)', tension: 0.0, fill: false } // Добавить новый датасет для GPU
            ]
        },
        options: {
            responsive: false,
            maintainAspectRatio: false,
            scales: {
                y: {
                    min: minValue,
                    max: 100,
                    ticks: {
                        display: true // Включить отображение подписей на оси Y
                    }
                },
                x: {
                    ticks: {
                        display: false // Включить отображение подписей на оси X
                    }
                }
            },
            plugins: {
                legend: {
                    display: true // Включить легенду для отображения названий графиков
                }
            }
        }
    });
}