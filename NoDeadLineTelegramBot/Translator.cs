using Python.Runtime;

public class Translator
{
    public static void InitializePythonEngine()
    {
        PythonEngine.Initialize();
        using (Py.GIL()) // Гарантирует, что код выполняется в правильном потоке
        {
            dynamic sys = Py.Import("sys");
            Console.WriteLine("Python version: " + sys.version);
        }
    }

    public static string Translate(string text)
    {
        using (Py.GIL()) // Управление GIL для работы с Python
        {
            dynamic transformers = Py.Import("transformers");
            dynamic tokenizer = transformers.MarianTokenizer.from_pretrained("Helsinki-NLP/opus-mt-ru-en");
            dynamic model = transformers.MarianMTModel.from_pretrained("Helsinki-NLP/opus-mt-ru-en");

            var inputs = tokenizer.prepare_seq2seq_batch(new[] { text }, return_tensors: "pt");
            var args = new PyDict();
            args["input_ids"] = inputs.input_ids;
            args["attention_mask"] = inputs.attention_mask;

            dynamic translated = model.generate(args);
            string translatedText = tokenizer.batch_decode(translated, new { skip_special_tokens = true })[0];
            return translatedText;
        }
    }
}