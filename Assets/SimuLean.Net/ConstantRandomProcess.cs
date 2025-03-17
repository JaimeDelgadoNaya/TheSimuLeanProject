using System;

namespace SimuLean
{
    /// <summary>
    /// Implementación de DoubleRandomProcess que siempre retorna un valor constante.
    /// </summary>
    public class ConstantRandomProcess : DoubleRandomProcess
    {
        private double constant;

        /// <summary>
        /// Constructor que recibe el valor constante.
        /// </summary>
        /// <param name="constant">El valor constante a retornar.</param>
        public ConstantRandomProcess(double constant)
        {
            this.constant = constant;
        }

        /// <summary>
        /// Inicialización (no se utiliza en este caso).
        /// </summary>
        public void Initialize(double initialValue, double[] parameters)
        {
            // Se ignoran los parámetros en esta implementación.
            constant = initialValue;
        }

        /// <summary>
        /// Retorna siempre el valor constante.
        /// </summary>
        public double NextValue()
        {
            return constant;
        }
    }
}
