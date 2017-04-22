using ConvNetSharp;
using ConvNetSharp.Training;
using ConvNetSharp.Layers;

namespace roadTrack
{
    public partial class Form1
    {
        private Net net;
        private AdadeltaTrainer trainer;

        // Ширина изображения
        int inputWidth = 320;
        // Высота изображения
        int inputHeight = 240;
        // Число каналов у изображения
        int inputDepth = 1;

        string[] names;

        private void CreateNetworkForTactile()
        {
            // Создаем сеть
            net = new Net();

            net.AddLayer(new InputLayer(inputWidth, inputHeight, inputDepth));

            // Ширина и высота рецептивного поля, количество фильтров
            net.AddLayer(new ConvLayer(3, 3, 8)
            {
                // Шаг скольжения свертки
                Stride = 1,
                // Заполнение краев нулями
                Pad = 1
            });
            net.AddLayer(new ReluLayer());

            // Ширина и высота окна уплотнения
            net.AddLayer(new PoolLayer(2, 2)
            {
                // Сдвиг
                Stride = 2
            });

            net.AddLayer(new ConvLayer(3, 3, 16)
            {
                Stride = 1,
                Pad = 1
            });
            net.AddLayer(new ReluLayer());

            net.AddLayer(new PoolLayer(3, 3)
            {
                Stride = 3
            });

            net.AddLayer(new FullyConnLayer(names.Length));
            net.AddLayer(new SoftmaxLayer(names.Length));
        }
    }
}