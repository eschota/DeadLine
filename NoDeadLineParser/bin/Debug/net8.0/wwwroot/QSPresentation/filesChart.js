document.addEventListener('DOMContentLoaded', function() {
    var ctx = document.getElementById('fileCountChart').getContext('2d');
    var fileCountChart = new Chart(ctx, {
        type: 'line',
        data: {
            labels: ['May 2023', 'June 2023', 'July 2023', 'August 2023', 'September 2023', 'October 2023', 'November 2023', 'December 2023', 'January 2024', 'February 2024', 'March 2024', 'April 2024'],
            datasets: [{
                label: 'Video',
                data: [10000, 20000, 30000, 50000, 80000, 100000, 130000, 160000, 190000, 220000, 250000, 280000],
                borderColor: 'rgb(255, 99, 132)',
                borderWidth: 2,
                tension: 0.4
            }, {
                label: 'Photo',
                data: [5000, 15000, 25000, 45000, 75000, 95000, 120000, 150000, 180000, 210000, 240000, 270000],
                borderColor: 'rgb(54, 162, 235)',
                borderWidth: 2,
                tension: 0.4

            }, {
                label: 'Vector',
                data: [2000, 8000, 18000, 28000, 38000, 58000, 78000, 98000, 118000, 138000, 158000, 178000],
                borderColor: 'rgb(75, 192, 192)',
                borderWidth: 2,
                tension: 0.4
            }, {
                label: 'Illustration',
                data: [1000, 4000, 9000, 14000, 19000, 24000, 29000, 34000, 39000, 44000, 49000, 54000],
                borderColor: 'rgb(153, 102, 255)',
                borderWidth: 2,
                tension: 0.4
            }, {
                label: 'Authors',
                data: [2000, 7000, 12000, 17000, 22000, 27000, 32000, 37000, 42000, 77000, 92000, 157000],
                borderColor: 'rgb(255, 159, 64)',
                borderWidth: 2,
                tension: 0.4
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            aspectRatio: 1,
            animation: {
                duration: 2000, // анимация появления графика длится 2 секунды
                easing: 'easeInOutQuart' // тип анимации
            },
            
            scales: {
                y: {
                    beginAtZero: true
                },
                x: {
                    grid: {
                        display: true,
                        color: 'rgba(255, 255, 255, 0.1)' // цвет разлиновки по оси X
                    }
                },
                y: {
                    grid: {
                        display: true,
                        color: 'rgba(255, 255, 255, 0.1)' // цвет разлиновки по оси Y
                    }
                }
            },
            plugins: {
                tooltip: {
                    enabled: true,
                    callbacks: {
                        label: function(tooltipItem) {
                            let label = tooltipItem.dataset.label || '';
                            let value = tooltipItem.parsed.y;
                            if (label === 'Authors') {
                                value = value / 100; // Деление значения на 10 для задач
                            }
                            return label + ': ' + value; // Форматируем вывод, добавляя два десятичных знака
                        }
                    }}
            }
        }
    });
});
