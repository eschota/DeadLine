function showChart(container,upData,downData, cpuData, gpuData, taskData, dateData) {
    var chartCanvas = container.parentElement.querySelector('canvas');
    chartCanvas.style.display = 'block';

    // Устанавливаем размеры canvas согласно родительскому контейнеру
    chartCanvas.width = container.offsetWidth;
    chartCanvas.height = container.offsetHeight;

    if (chartCanvas.chart) {
        chartCanvas.chart.destroy();
    }

    var minValue = Math.min(...cpuData, ...gpuData);
    var maxValue = Math.max( ...taskData);

    var ctx = chartCanvas.getContext('2d');
    chartCanvas.chart = new Chart(ctx, {
        type: 'line',
        data: {
            labels: dateData,
            datasets: [
                { label: 'CPU', data: cpuData, borderColor: 'rgb(75, 192, 192)', tension: 0.0, fill: false },
                { label: 'GPU', data: gpuData, borderColor: 'rgb(192, 75, 192)', tension: 0.0, fill: false },
                { label: 'Upload', data: upData, borderColor: 'rgb(128, 32, 64)', tension: 0.0, fill: false },
                { label: 'Download', data: downData, borderColor: 'rgb(192, 75, 75)', tension: 0.0, fill: false },
                { label: 'Tasks', data: taskData, borderColor: 'rgb(192, 192, 75)', tension: 0.0, fill: false } // Новый датасет для отображения выполненных задач
            ]
        },
        options: {
            animation: false,
            responsive: false,
            maintainAspectRatio: false,
            scales: {
                y: {
                    min: 0,
                    max: 100, // Изменено для корректного отображения максимального значения с учетом taskData
                    ticks: {callback: function(value, index, ticks) {
                        return '$' + value;
                    },
                        display: false // Включить отображение подписей на оси Y
                    }
                },
                x: {
                    ticks: {
                        display: false // Изменено для включения отображения подписей на оси X
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