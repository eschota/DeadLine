function animateBlocks(direction) {
    const blocks = document.querySelectorAll('.animated-block');
    blocks.forEach(block => {
        console.log("direction"+direction);
        
        block.style.transition = 'none'; // Отключаем анимацию для мгновенного перемещения
        block.style.transform = `translateY(${direction * 200}%)`; // Мгновенное начальное положение

        setTimeout(() => {
            block.style.transition = 'transform 500ms ease-in-out'; // Включаем анимацию
            block.style.transform = `translateY(${-direction * 200}%)`; // Запуск анимации
        }, 100); // Микрозадержка для разделения установки начальной позиции и начала анимации
    });
}