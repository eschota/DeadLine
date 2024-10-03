using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
 
  
    public class Vectors
    {
        // Метод для расчета косинусного сходства между двумя векторами
        public static double CosineSimilarity(float[] vectorA, float[] vectorB)
        {
            if (vectorA.Length != vectorB.Length)
                throw new ArgumentException("Vectors must be of the same length");

            double dotProduct = vectorA.Zip(vectorB, (a, b) => a * b).Sum();
            double normA = Math.Sqrt(vectorA.Sum(x => x * x));
            double normB = Math.Sqrt(vectorB.Sum(x => x * x));

            if (normA == 0 || normB == 0)
                return 0; // to handle the case of zero vectors

            return dotProduct / (normA * normB);
        }

        // Метод для расчета косинусного расстояния между двумя векторами
        public static double CosineDistance(float[] vectorA, float[] vectorB)
        {
            double similarity = CosineSimilarity(vectorA, vectorB);
            return 1 - similarity; // Косинусное расстояние = 1 - косинусное сходство
        }
    }