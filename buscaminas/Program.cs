using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

class Program
{
    static async Task Main()
    {
       // Console.SetWindowSize(120, 40);
        //Console.SetBufferSize(200, 60);
        var simulacion = new Simulacion();
        await simulacion.EjecutarAsync();
    }
}

class Campo
{
    private bool[,] tablero;
    private readonly object candado = new object();
    public int MinasRestantes { get; private set; }
    public int Tamaño { get; }

    public Campo(int tamaño, int cantidadMinas)
    {
        Tamaño = tamaño;
        tablero = new bool[tamaño, tamaño];
        MinasRestantes = cantidadMinas;
        ColocarMinas(cantidadMinas);
    }

    private void ColocarMinas(int cantidad)
    {
        Random random = new Random();
        int minasColocadas = 0;
        while (minasColocadas < cantidad)
        {
            int x = random.Next(Tamaño);
            int y = random.Next(Tamaño);
            if (!tablero[x, y])
            {
                tablero[x, y] = true;
                minasColocadas++;
            }
        }
    }

    public bool HayMina(int x, int y) => tablero[x, y];

    public bool DesactivarMina(int x, int y)
    {
        lock (candado)
        {
            if (tablero[x, y])
            {
                tablero[x, y] = false;
                MinasRestantes--;
                return true;
            }
            return false;
        }
    }

    public async Task DibujarCampo()
    {
        Console.SetCursorPosition(0, 0);
        List<string> mines = new List<string>();
        int t = Tamaño;
        int t2 = Tamaño;
        for (int y = 1; y <= t; y++)
        {
            for (int x = 1; x <= t2; x++)
            {
                if (tablero[x-1, y-1])
                {
                    Console.Write("M");
                    mines.Add($"{x},{y}");
                }
                else
                {
                    Console.Write("*");
                }
            }
            Console.WriteLine();
        }
    }
}

class Persona
{
    public int PosX { get; private set; }
    public int PosY { get; private set; }
    public bool EstaViva { get; private set; } = true;
    public int Id { get; }
    private readonly Campo campo;
    private static readonly Random random = new Random();

    public Persona(Campo campo, int id)
    {
        this.campo = campo;
        Id = id;
        PosX = random.Next(campo.Tamaño);
        PosY = 0;
    }

    public async Task CruzarCampoAsync()
    {
        while (PosY < campo.Tamaño - 2 && EstaViva)
        {
            PosY++;
            if (campo.HayMina(PosX, PosY))
            {
                EstaViva = false;
                Console.SetCursorPosition(PosX, PosY);
                Console.Write("X");

                break;
            }
            else
            {
                Console.SetCursorPosition(PosX, PosY);
                Console.Write("S");
            }
            await Task.Delay(50);
        }
        if (EstaViva)
        {
            Console.SetCursorPosition(PosX * 4, PosY);
            Console.Write("S");
        }
    }
}

class Desactivador
{
    private readonly Campo campo;
    public int MinasDesactivadas { get; private set; } = 0;
    public int Id { get; }
    private static readonly Random random = new Random();

    public Desactivador(Campo campo, int id)
    {
        this.campo = campo;
        Id = id;
    }

    public async Task BuscarMinasAsync()
    {
        while (campo.MinasRestantes > 0)
        {
            int x = random.Next(campo.Tamaño);
            int y = random.Next(campo.Tamaño);
            if (campo.DesactivarMina(x, y))
            {
                MinasDesactivadas++;
                Console.SetCursorPosition(x * 3, y);
                Console.Write("   ");
            }
            await Task.Delay(30);
        }
    }
}

class Simulacion
{
    private readonly Campo campo;
    private readonly List<Persona> personas;
    private readonly List<Desactivador> desactivadores;

    public Simulacion()
    {
        campo = new Campo(100, 50);
        personas = Enumerable.Range(1, 15).Select(id => new Persona(campo, id)).ToList();
        desactivadores = Enumerable.Range(1, 10).Select(id => new Desactivador(campo, id)).ToList();
    }

    public async Task EjecutarAsync()
    {
        Console.Clear();
       await campo.DibujarCampo();

        var reloj = System.Diagnostics.Stopwatch.StartNew();
        var tareasPersonas = personas.Select(p => p.CruzarCampoAsync()).ToList();
        var tareasDesactivadores = desactivadores.Select(d => d.BuscarMinasAsync()).ToList();
        await Task.WhenAll(tareasPersonas.Concat(tareasDesactivadores));
        reloj.Stop();

        Console.SetCursorPosition(0, campo.Tamaño + 2);
        Console.WriteLine("\n--- Resultados ---");
        Console.WriteLine($"Tiempo total: {reloj.Elapsed.TotalSeconds:F2} segundos");
        Console.WriteLine($"Minas desactivadas: {desactivadores.Sum(d => d.MinasDesactivadas)}");
        Console.WriteLine($"Personas sobrevivientes: {personas.Count(p => p.EstaViva)}");
        Console.WriteLine($"Personas eliminadas: {personas.Count(p => !p.EstaViva)}");
    }
}
