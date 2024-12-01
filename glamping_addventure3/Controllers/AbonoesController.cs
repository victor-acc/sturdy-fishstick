using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using glamping_addventure3.Models;

namespace glamping_addventure3.Controllers
{
    public class AbonoesController : Controller
    {
        private readonly GlampingAddventure3Context _context;

        public AbonoesController(GlampingAddventure3Context context)
        {
            _context = context;
        }

        // GET: Abonoes
        public async Task<IActionResult> Index(int? idReserva)
        {
            if (idReserva == null)
            {
                return NotFound();
            }

            var abonos = _context.Abonos
                .Include(a => a.IdreservaNavigation)
                .Where(a => a.Idreserva == idReserva);

            ViewData["IdReservaActual"] = idReserva; // Pasar el ID de la reserva actual a la vista.
            return View(await abonos.ToListAsync());
        }

        // GET: Abonoes/Details/5
        public async Task<IActionResult> Details(int? id, int? idReserva)
        {
            if (id == null)
            {
                return NotFound();
            }

            var abono = await _context.Abonos
                .Include(a => a.IdreservaNavigation)
                .FirstOrDefaultAsync(m => m.Idabono == id);

            if (abono == null)
            {
                return NotFound();
            }
            ViewData["IdReserva"] = idReserva;

            ViewData["Comprobante"] = abono.Comprobante != null
                ? $"data:image/png;base64,{Convert.ToBase64String(abono.Comprobante)}"
                : null;

            return View(abono);
        }

        // GET: Abonoes/Create
        public IActionResult Create(int? idReserva)
        {
            if (idReserva == null)
            {
                return NotFound();
            }

            var reserva = _context.Reservas.Find(idReserva);
            if (reserva == null)
            {
                return NotFound();
            }

            ViewData["IdReserva"] = idReserva;
            ViewData["MontoTotal"] = reserva.MontoTotal; // Suponiendo que la tabla Reserva tiene un campo MontoTotal.
            return View();
        }


        // POST: Abonoes/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Idabono,Idreserva,FechaAbono,ValorDeuda,Porcentaje,Pendiente,CantAbono,Estado")] Abono abono, IFormFile comprobante)
        {
            // Obtener reserva asociada
            var reserva = await _context.Reservas
                .Include(r => r.Abonos)
                .FirstOrDefaultAsync(r => r.IdReserva == abono.Idreserva);

            if (reserva == null)
            {
                ModelState.AddModelError("", "La reserva no existe.");
                return View(abono);
            }

            // Calcular total de abonos previos, excluyendo los anulados
            var totalAbonosPrevios = reserva.Abonos
                .Where(a => !a.Estado) // Excluir abonos en estado "Anulado"
                .Sum(a => a.CantAbono);

            // Calcular pendiente actual
            abono.Pendiente = reserva.MontoTotal - totalAbonosPrevios;

            // Calcular porcentaje del abono actual sobre el monto total de la reserva
            abono.Porcentaje = (abono.CantAbono / reserva.MontoTotal) * 100;

            // Validaciones
            if (abono.Pendiente == reserva.MontoTotal && abono.Porcentaje < 50)
            {
                ModelState.AddModelError("", "El primer abono debe ser al menos el 50% del valor de la deuda.");
            }

            if ((totalAbonosPrevios + abono.CantAbono) > reserva.MontoTotal)
            {
                ModelState.AddModelError("", "No se pueden realizar más abonos, ya se ha completado el 100%.");
            }

            if (ModelState.IsValid)
            {
                // Procesar comprobante
                if (comprobante != null && comprobante.Length > 0)
                {
                    using (var memoryStream = new MemoryStream())
                    {
                        await comprobante.CopyToAsync(memoryStream);
                        abono.Comprobante = memoryStream.ToArray(); // Convertir la imagen a binario
                    }
                }

                // Guardar abono
                _context.Add(abono);
                await _context.SaveChangesAsync();

                // Redirige al índice de abonos para la reserva específica
                return RedirectToAction(nameof(Index), new { idReserva = abono.Idreserva });
            }

            // Asegúrate de que los datos necesarios estén disponibles para la vista
            ViewData["IdReserva"] = abono.Idreserva;
            ViewData["MontoTotal"] = reserva.MontoTotal;

            return View(abono);
        }
        // POST: Abonoes/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Idabono,Idreserva,FechaAbono,ValorDeuda,Porcentaje,Pendiente,CantAbono,Comprobante,Estado")] Abono abono)
        {
            if (id != abono.Idabono)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(abono);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!AbonoExists(abono.Idabono))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["Idreserva"] = new SelectList(_context.Reservas, "IdReserva", "IdReserva", abono.Idreserva);
            return View(abono);
        }

        // GET: Abonoes/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var abono = await _context.Abonos
                .Include(a => a.IdreservaNavigation)
                .FirstOrDefaultAsync(m => m.Idabono == id);
            if (abono == null)
            {
                return NotFound();
            }

            return View(abono);
        }

        // POST: Abonoes/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var abono = await _context.Abonos.FindAsync(id);
            if (abono != null)
            {
                _context.Abonos.Remove(abono);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool AbonoExists(int id)
        {
            return _context.Abonos.Any(e => e.Idabono == id);
        }
    }
}
