using System;

namespace SimuLean
{
    /// <summary>
    /// Clase abstracta que define la estrategia de retardo.
    /// </summary>
    public abstract class DelayStrategy
    {
        /// <summary>
        /// Calcula y retorna el retardo en función del item recibido.
        /// </summary>
        /// <param name="theItem">El item para el que se calcula el retardo.</param>
        /// <returns>Retardo en segundos (float).</returns>
        public abstract float GetDelay(Item theItem);
    }

    /// <summary>
    /// Estrategia de retardo que utiliza un generador aleatorio.
    /// Si se pasa un único valor (delay), se utiliza como retardo constante.
    /// Si se pasan dos valores, se toma un número aleatorio en ese rango.
    /// </summary>
    public class RandomDelayStrategy : DelayStrategy
    {
        private Random random;
        private float minDelay;
        private float maxDelay;

        /// <summary>
        /// Constructor para retardo constante.
        /// </summary>
        /// <param name="delay">Retardo constante.</param>
        public RandomDelayStrategy(float delay)
        {
            this.minDelay = delay;
            this.maxDelay = delay;
            this.random = new Random();
        }

        /// <summary>
        /// Constructor para retardo aleatorio en un rango.
        /// </summary>
        /// <param name="minDelay">Retardo mínimo.</param>
        /// <param name="maxDelay">Retardo máximo.</param>
        public RandomDelayStrategy(float minDelay, float maxDelay)
        {
            this.minDelay = minDelay;
            this.maxDelay = maxDelay;
            this.random = new Random();
        }

        public override float GetDelay(Item theItem)
        {
            // Si el retardo es constante, retorna ese valor.
            if (minDelay == maxDelay)
                return minDelay;
            // Sino, retorna un valor aleatorio entre minDelay y maxDelay.
            return (float)(minDelay + random.NextDouble() * (maxDelay - minDelay));
        }
    }

    /// <summary>
    /// Estrategia de retardo que calcula el retardo evaluando una expresión.
    /// Se puede utilizar una librería externa (como NCalc) para evaluar expresiones complejas.
    /// En este ejemplo, si la expresión se puede convertir a float se utiliza ese valor.
    /// </summary>
    public class ExpressionDelayStrategy : DelayStrategy
    {
        private string expression;

        /// <summary>
        /// Constructor que recibe la expresión.
        /// </summary>
        /// <param name="expression">Expresión en forma de string.</param>
        public ExpressionDelayStrategy(string expression)
        {
            this.expression = expression;
        }

        public override float GetDelay(Item theItem)
        {
            // Ejemplo básico: se intenta convertir la expresión a float.
            // En una implementación real, podrías evaluar la expresión usando, por ejemplo, NCalc.
            if (float.TryParse(expression, out float delay))
            {
                return delay;
            }
            else
            {
                throw new ArgumentException($"No se pudo evaluar la expresión '{expression}' a un float.");
            }
        }
    }
}
