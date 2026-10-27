using SVR.Infraestructura;

namespace SVR.Pruebas;

public class LectorCsvPruebas
{
    [Fact]
    public void Leer_SaltaEncabezadoYLineasVacias_YConservaElNumeroDeLinea()
    {
        //PATRON AAA

        //Act
        string ruta = Path.GetTempFileName();
        File.WriteAllLines(ruta, ["ean,precio", "7501055300075, 18.50", "", "7501000111206,abc"]);

        //Arrange
        var filas = LectorCsv.Leer(ruta).ToList();
        File.Delete(ruta);

        //Assert
        Assert.Equal(2, filas.Count);
        Assert.Equal(2, filas[0].Linea);
        Assert.Equal("18.50", filas[0].Campos[1]);   // sin espacios
        Assert.Equal(4, filas[1].Linea);             // la línea vacía cuenta para el número
    }

    //  agrega aquí pruebas de tus promociones y de ServicioVenta
    // usando implementaciones falsas de IInventarioRepo (como en el Módulo 8).
}
