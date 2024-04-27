function showChart(container, posData, dateData) {
    console.log('Initializing chart display...');
    var chartCanvas = container.parentElement.querySelector('canvas');
    chartCanvas.style.display = 'block';

    console.log('Setting canvas dimensions...');
    chartCanvas.width = container.offsetWidth;
    chartCanvas.height = container.offsetHeight;

    if (chartCanvas.chart) {
        console.log('Destroying existing chart...');
        chartCanvas.chart.destroy();
    }

    console.log('Calculating min and max values...');
    var minValue = Math.min(...posData);
    var maxValue = Math.max(...posData);

    var ctx = chartCanvas.getContext('2d');
    console.log('Creating new chart...');
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
                    min: minValue,
                    max: maxValue,
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
    console.log('Chart created successfully.');
}

function hideChart(container) {
    console.log('Hiding chart...');
    var chartCanvas = container.parentElement.querySelector('canvas');
    if (chartCanvas) {
        chartCanvas.style.display = 'none';
        console.log('Chart hidden.');
    } else {
        console.log('No chart to hide.');
    }
}
function Hello() {
    console.log('Hello Suka...');    
}
