function updateCharts() {
    const nodes = document.querySelectorAll('.product');

    nodes.forEach(node => {
        const canvasId = node.querySelector('canvas').id;
        const nodeName = canvasId.replace('chart', '').replace('_', ' ');
        
        fetch(`/api/chartData/${nodeName}`)
            .then(response => response.json())
            .then(data => {
                const container = document.getElementById(canvasId);
                showChart(container, data.data, data.labels);
            })
            .catch(error => console.error('Error updating chart:', error));
    });
}

// Запустить обновление данных каждые 5 минут (300000 миллисекунд)
setInterval(updateCharts, 5000);
