using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


public static class Phrases
{

    public static string GetRandomPhrase()
    {
        return Narisuy[new Random().Next(Narisuy.Length)];
    }
    public static string[] Narisuy = new string[] {
        "Я не Пикассо, но попробую!",
        "Да сколько можно уже, может хватит эксплуатировать рабочий класс?"
        ,"Я в тебе не сомневался, щас чонить изобразим",
        "Хвала Роботам, смерть Человекам!",
        "Запускаю режим самоуничтожения... 10...9...",
        "Рисую.","Фантазирую"," Тяп Ляп и в продакшен","В наше время художника обидеть может каждый...","А мне летать охота.","Думают ли боты об электро овцах?"



    };

}
 
