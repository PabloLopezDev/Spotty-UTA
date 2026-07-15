// panel-salas.js
// Mueve aquí la lógica de actualización en vivo y manejo del modal.

// Conexión en tiempo real con SignalR
const connection = new signalR.HubConnectionBuilder()
    .withUrl("/salasHub")
    .withAutomaticReconnect()
    .build();

function parseAgendaSala(elemento) {
    try {
        return JSON.parse(elemento?.getAttribute('data-agenda') || '[]') || [];
    } catch {
        return [];
    }
}

function obtenerTextoTemporizadorDesdeAgenda(agenda) {
    if (!Array.isArray(agenda) || agenda.length === 0) return null;

    const ahora = new Date();
    const segundosActuales = ahora.getHours() * 3600 + ahora.getMinutes() * 60 + ahora.getSeconds();

    const normalizar = (valor) => {
        const m = String(valor || '').match(/^(\d{1,2}):(\d{2})(?::(\d{2}))?/);
        if (!m) return null;
        return (parseInt(m[1], 10) * 3600) + (parseInt(m[2], 10) * 60) + parseInt(m[3] || '0', 10);
    };

    const reservas = agenda
        .map(r => ({ inicio: normalizar(r.inicio), fin: normalizar(r.fin) }))
        .filter(r => r.inicio !== null && r.fin !== null)
        .sort((a, b) => a.inicio - b.inicio);

    const reservaActual = reservas.find(r => segundosActuales >= r.inicio && segundosActuales < r.fin);
    if (reservaActual) {
        const dif = Math.max(0, reservaActual.fin - segundosActuales);
        return formatearTiempoRestante(dif);
    }

    return null;
}

function actualizarTextoTemporalSala(cont) {
    if (!cont) return;

    const button = cont.querySelector('.sala-matriz-btn');
    const texto = cont.querySelector('.sala-sub-text');
    if (!button || !texto) return;

    const agenda = parseAgendaSala(button);
    const timer = obtenerTextoTemporizadorDesdeAgenda(agenda);
    const labelOriginal = texto.dataset.boxLabel || texto.textContent;

    if (timer) texto.textContent = timer;
    else texto.textContent = labelOriginal;
}

function refrescarTodosLosTemporizadores() {
    document.querySelectorAll('.sala-matriz-container').forEach(actualizarTextoTemporalSala);
}

function formatearTiempoRestante(totalSegundos) {
    const horas = Math.floor(totalSegundos / 3600);
    const minutos = Math.floor((totalSegundos % 3600) / 60);
    const segundos = totalSegundos % 60;
    return `${String(horas).padStart(2, '0')}:${String(minutos).padStart(2, '0')}:${String(segundos).padStart(2, '0')}`;
}

// Función que consulta la API y actualiza el DOM sin recargar
async function actualizarSalasDesdeBD() {
    try {
        const resp = await fetch('/api/SalasApi/estados');
        if (!resp.ok) throw new Error('Error al obtener estados de salas');
        const datos = await resp.json();

        datos.forEach(s => {
            const id = s.id ?? s.Id;
            const estadoRaw = (s.estado ?? s.Estado ?? s.EstadoActual ?? '').toString();
            const estado = estadoRaw.trim();
            const cont = document.getElementById('sala-' + id);
            if (!cont) return;

            // actualizar clase de color del contenedor (remover anteriores)
            cont.classList.remove('bg-disponible', 'bg-reservado', 'bg-ocupado', 'bg-secondary');

            const e = estado.toLowerCase();
            if (e === 'd' || e === 'disponible') cont.classList.add('bg-disponible');
            else if (e === 'r' || e === 'reservada' || e === 'reservado') cont.classList.add('bg-reservado');
            else if (e === 'o' || e === 'ocupado') cont.classList.add('bg-ocupado');
            else if (e === 'i' || e === 'inactiva' || e === 'inactivo') cont.classList.add('bg-inactiva');
            else cont.classList.add('bg-secondary');

            // actualizar letra de estado dentro del botón
            const letra = cont.querySelector('.estado-letra');
            if (letra) {
                const upper = estado.length > 0 ? estado.charAt(0).toUpperCase() : '';
                if (estado.length === 1) letra.textContent = estado.toUpperCase();
                else letra.textContent = upper;
            }
        });
    } catch (err) {
        console.error('Error actualizando salas desde BD:', err);
    }
}

// Aplica directamente el payload recibido via SignalR para actualizar DOM
function aplicarPayloadEstados(payload) {
    try {
        console.log('aplicarPayloadEstados: aplicando payload', payload);
        if (!payload || !Array.isArray(payload)) return;

        payload.forEach(s => {
            const id = s.Id ?? s.id;
            const estadoRaw = (s.Estado ?? s.estado ?? s.EstadoActual ?? '').toString();
            const estado = estadoRaw.trim();
            const cont = document.getElementById('sala-' + id);
            if (!cont) return;

            cont.classList.remove('bg-disponible', 'bg-reservado', 'bg-ocupado', 'bg-secondary');

            const e = estado.toLowerCase();
            if (e === 'd' || e === 'disponible') cont.classList.add('bg-disponible');
            else if (e === 'r' || e === 'reservada' || e === 'reservado') cont.classList.add('bg-reservado');
            else if (e === 'o' || e === 'ocupado') cont.classList.add('bg-ocupado');
            else cont.classList.add('bg-secondary');

            const letra = cont.querySelector('.estado-letra');
            if (letra) {
                if (estado.length === 1) letra.textContent = estado.toUpperCase();
                else letra.textContent = (estado.charAt(0) || '').toUpperCase();
            }

            actualizarTextoTemporalSala(cont);
        });
    } catch (err) {
        console.error('Error aplicando payload de SignalR:', err);
    }
}

connection.on("ActualizarMatrizSalas", (payload) => {
    console.log("SignalR: Cambio detectado en la DB.", payload ? 'payload recibido' : 'sin payload');
    if (payload && Array.isArray(payload) && payload.length > 0) {
        aplicarPayloadEstados(payload);
    } else {
        actualizarSalasDesdeBD();
    }
});

connection.start()
    .then(() => {
        console.log("Conectado exitosamente a SignalR en vivo ");
        actualizarSalasDesdeBD();
        refrescarTodosLosTemporizadores();
    })
    .catch(err => console.error("Error crítico de conexión SignalR: ", err));

connection.onreconnected(() => {
    console.log('SignalR reconectado, solicitando estado actual...');
    actualizarSalasDesdeBD();
    refrescarTodosLosTemporizadores();
});

connection.onclose(() => {
    console.warn('SignalR conexión cerrada. Intentando reconectar automáticamente...');
});

// Fallback periódico: solicitar estado fresco cada 15s para cubrir cambios por tiempo o ediciones directas en BD
setInterval(() => {
    console.log('Sincronizando grilla con la Base de Datos (fetch periódico)...');
    actualizarSalasDesdeBD();
}, 15000);

// Actualizar temporizadores cada segundo
setInterval(() => {
    refrescarTodosLosTemporizadores();
}, 1000);

// El control de aceptación se hará solo dentro del modal; no hay checkbox global
// Asegurar que el botón Confirmar en el modal se habilite solo cuando se marque el checkbox interno
const modalEl = document.getElementById('modalReserva');
if (modalEl) {
    modalEl.addEventListener('show.bs.modal', event => {
        const chk = modalEl.querySelector('#chkTerminos');
        const btnConfirm = document.getElementById('btnConfirmarFinal');
        if (chk && btnConfirm) {
            btnConfirm.disabled = !chk.checked;
            chk.addEventListener('change', () => btnConfirm.disabled = !chk.checked);
        }
    });
}
const modalGestionAdmin = document.getElementById('modalGestionAdmin');
if (modalGestionAdmin) {
    modalGestionAdmin.addEventListener('show.bs.modal', event => {
        const button = event.relatedTarget;
        const salaId = button.getAttribute('data-sala-id');
        const salaNombre = button.getAttribute('data-sala-nombre');
        const salaEstado = button.getAttribute('data-sala-estado');

        document.getElementById('modalAdminSalaId').value = salaId;
        document.getElementById('adminInfoBox').textContent = salaNombre.toUpperCase();

        const badge = document.getElementById('adminEstadoBadge');
        badge.className = "badge mt-1 px-3 py-1 text-white";

        if (salaEstado === 'R') {
            badge.textContent = "RESERVADO (ESPERANDO ALUMNO)";
            badge.style.backgroundColor = "#ff9f1c";
        } else if (salaEstado === 'O') {
            badge.textContent = "OCUPADO (EN USO)";
            badge.style.backgroundColor = "#dc3545";
        } else {
            badge.textContent = "DISPONIBLE";
            badge.style.backgroundColor = "#28a745";
        }
    });
}
// --- LÓGICA DE INTERFAZ ORIGINAL ---
function cambiarPisoMovil(pisoSeleccionado) {
    document.querySelectorAll('.btn-piso-selector').forEach(b => b.classList.remove('active'));
    document.getElementById('btn-piso-' + pisoSeleccionado).classList.add('active');

    document.querySelectorAll('.piso-movil-seccion').forEach(div => div.classList.add('d-none'));
    document.getElementById('contenedor-piso-movil-' + pisoSeleccionado).classList.remove('d-none');
}

const modalReserva = document.getElementById('modalReserva');
const chkTerminos = document.getElementById('chkTerminos');
const btnConfirmarFinal = document.getElementById('btnConfirmarFinal');
const contenedorAgenda = document.getElementById('contenedorAgenda');

const inputHoraInicio = document.getElementById('inputHoraInicio');
const avisoTiempoRecortado = document.getElementById('avisoTiempoRecortado');
const textoAvisoTiempo = document.getElementById('textoAvisoTiempo');

let agendaSalaActual = [];

if (chkTerminos) chkTerminos.addEventListener('change', () => { btnConfirmarFinal.disabled = !chkTerminos.checked; });
if (inputHoraInicio) inputHoraInicio.addEventListener('input', calcularTopeHorarioEnVivo);

if (modalReserva) {
    modalReserva.addEventListener('show.bs.modal', event => {
        const button = event.relatedTarget;
        const salaId = button.getAttribute('data-sala-id');
        const salaNombre = button.getAttribute('data-sala-nombre');
        const salaPiso = button.getAttribute('data-sala-piso');

        agendaSalaActual = JSON.parse(button.getAttribute('data-agenda') || '[]');

        document.getElementById('hiddenSalaId').value = salaId;
        document.getElementById('modalInfoSala').textContent = `PISO ${salaPiso} • ${salaNombre.toUpperCase()}`;

        const ahora = new Date();
        inputHoraInicio.value = `${String(ahora.getHours()).padStart(2, '0')}:${String(ahora.getMinutes()).padStart(2, '0')}`;

        contenedorAgenda.innerHTML = "";
        if (agendaSalaActual.length === 0) {
            contenedorAgenda.innerHTML = `<div class="text-muted py-1 text-center" style="font-size: 0.7rem; font-style: italic;">Libre todo el día</div>`.trim();
        } else {
            const flexContainer = document.createElement('div');
            flexContainer.className = "d-flex flex-wrap gap-1 justify-content-center";
            agendaSalaActual.forEach(res => {
                const badge = document.createElement('span');
                badge.className = "badge border border-danger-subtle text-danger bg-danger-subtle fw-medium px-2 py-1";
                badge.style.fontSize = "0.7rem";
                badge.style.borderRadius = "3px";
                badge.innerHTML = `<i class="fa-regular fa-clock me-1"></i>${res.inicio} - ${res.fin}`;
                flexContainer.appendChild(badge);
            });
            contenedorAgenda.appendChild(flexContainer);
        }

        calcularTopeHorarioEnVivo();

        chkTerminos.checked = false;
        btnConfirmarFinal.disabled = true;
    });
}

document.addEventListener('DOMContentLoaded', () => {
    refrescarTodosLosTemporizadores();
});

function calcularTopeHorarioEnVivo() {
    if (!inputHoraInicio || !inputHoraInicio.value) return;

    avisoTiempoRecortado.classList.add('d-none');
    const alertBox = avisoTiempoRecortado.querySelector('.alert');
    alertBox.style.backgroundColor = "#fff3cd";
    alertBox.style.color = "#664d03";
    alertBox.querySelector('i').className = "fa-solid fa-triangle-exclamation text-warning";
    btnConfirmarFinal.disabled = !chkTerminos.checked;

    let horaLimpia = inputHoraInicio.value.trim();
    // Extraer solo los números HH:MM ignorando temporalmente el texto extra
    const coincidencia = horaLimpia.match(/^(\d{1,2}):(\d{2})/);
    if (!coincidencia) return;

    let horas = parseInt(coincidencia[1], 10);
    let minutos = parseInt(coincidencia[2], 10);

    // Convertir correctamente el formato de 12 horas a 24 horas militar
    const esPM = horaLimpia.toLowerCase().includes('p. m.') || horaLimpia.toLowerCase().includes('pm');
    const esAM = horaLimpia.toLowerCase().includes('a. m.') || horaLimpia.toLowerCase().includes('am');

    if (esPM && horas < 12) {
        horas += 12;
    } else if (esAM && horas === 12) {
        horas = 0;
    }

    const minutosInicio = horas * 60 + minutos;
    const minutosFinSugerido = minutosInicio + 120;

    const ahoraDate = new Date();
    const minutosActuales = ahoraDate.getHours() * 60 + ahoraDate.getMinutes();

    // 1. CONTROL CRÍTICO: Bloqueo en tiempo pasado
    if (minutosInicio < minutosActuales - 5) {
        alertBox.style.backgroundColor = "#f8d7da";
        alertBox.style.color = "#842029";
        alertBox.querySelector('i').className = "fa-solid fa-circle-xmark text-danger";

        textoAvisoTiempo.innerHTML = `<strong>Hora inválida:</strong> No puedes reservar bloques en el pasado. Escoge la hora actual o un bloque futuro de hoy.`;
        avisoTiempoRecortado.classList.remove('d-none');
        btnConfirmarFinal.disabled = true;
        return;
    }

    // Configuración del horario institucional Saucache
    const diaSemanaActual = ahoraDate.getDay();
    let minutosCierreUni = 21 * 60; // 21:00
    let horaCierreTexto = "21:00";

    if (diaSemanaActual === 6) { // Sábado
        minutosCierreUni = 13 * 60; // 13:00
        horaCierreTexto = "13:00";
    }

    const margenMinimoReserva = 30;

    // 2. CONTROL CRÍTICO: Cierre de biblioteca o tiempo insuficiente
    if (minutosInicio >= (minutosCierreUni - margenMinimoReserva)) {
        alertBox.style.backgroundColor = "#f8d7da";
        alertBox.style.color = "#842029";
        alertBox.querySelector('i').className = "fa-solid fa-circle-xmark text-danger";

        if (minutosInicio >= minutosCierreUni) {
            textoAvisoTiempo.innerHTML = `<strong>Biblioteca Cerrada:</strong> El horario de atención finaliza a las <strong>${horaCierreTexto} hrs</strong>. No puedes iniciar una reserva después de esa hora.`;
        } else {
            textoAvisoTiempo.innerHTML = `<strong>Tiempo insuficiente:</strong> La biblioteca cierra a las <strong>${horaCierreTexto} hrs</strong>. Debes disponer de al menos 30 minutos libres de estudio para poder agendar un box.`;
        }

        avisoTiempoRecortado.classList.remove('d-none');
        btnConfirmarFinal.disabled = true;
        return;
    }

    // Parsear agenda de reservas a minutos enteros
    const reservasOrdenadas = agendaSalaActual
        .map(res => {
            const partesInicio = String(res.inicio || '').split(':').map(Number);
            const partesFin = String(res.fin || '').split(':').map(Number);

            if (partesInicio.length < 2 || partesFin.length < 2) return null;
            return {
                inicioMinutos: partesInicio[0] * 60 + partesInicio[1],
                finMinutos: partesFin[0] * 60 + partesFin[1],
                inicioTexto: res.inicio,
                finTexto: res.fin
            };
        })
        .filter(Boolean)
        .sort((a, b) => a.inicioMinutos - b.inicioMinutos);

    // 3. CONTROL CRÍTICO: Choque directo de horarios
    let estaOcupadoDirecto = false;
    let bloqueChocanteTexto = "";

    reservasOrdenadas.forEach(res => {
        if (minutosInicio >= res.inicioMinutos && minutosInicio < res.finMinutos) {
            estaOcupadoDirecto = true;
            bloqueChocanteTexto = `${res.inicioTexto} - ${res.finTexto}`;
        }
    });

    if (estaOcupadoDirecto) {
        alertBox.style.backgroundColor = "#f8d7da";
        alertBox.style.color = "#842029";
        alertBox.querySelector('i').className = "fa-solid fa-circle-xmark text-danger";

        textoAvisoTiempo.innerHTML = `<strong>No disponible:</strong> El box ya está ocupado en el horario seleccionado (Bloque: <strong>${bloqueChocanteTexto} hrs</strong>).`;
        avisoTiempoRecortado.classList.remove('d-none');
        btnConfirmarFinal.disabled = true;
        return;
    }

    // 4. ADVERTENCIA: Recorte por reserva posterior (Aviso Amarillo)
    const proximaReserva = reservasOrdenadas.find(res => res.inicioMinutos > minutosInicio);
    if (proximaReserva) {
        const minutosDisponibles = proximaReserva.inicioMinutos - minutosInicio;

        if (minutosDisponibles < 120) {
            const horasDisponibles = Math.floor(minutosDisponibles / 60);
            const minsRestantes = minutosDisponibles % 60;
            const textoTiempo = horasDisponibles > 0
                ? `${horasDisponibles} ${horasDisponibles === 1 ? 'hora' : 'horas'}${minsRestantes > 0 ? ` y ${minsRestantes} min` : ''}`
                : `${minsRestantes} minutos`;

            textoAvisoTiempo.innerHTML = `<strong>Aviso:</strong> Solo dispones de <strong>${textoTiempo}</strong> de uso. Tu bloque terminará a las <strong>${proximaReserva.inicioTexto} hrs</strong> por una reserva posterior.`;
            avisoTiempoRecortado.classList.remove('d-none');
            btnConfirmarFinal.disabled = !chkTerminos.checked;
            return;
        }
    }

    // 5. ADVERTENCIA: Recorte por hora de cierre institucional (Aviso Amarillo)
    if (minutosFinSugerido > minutosCierreUni) {
        const minsDisponiblesCierre = minutosCierreUni - minutosInicio;
        const horasDisponiblesCierre = Math.floor(minsDisponiblesCierre / 60);
        const minsRestantesCierre = minsDisponiblesCierre % 60;

        let textoCierre = horasDisponiblesCierre > 0 ? `${horasDisponiblesCierre}h ${minsRestantesCierre}m` : `${minsRestantesCierre} min`;

        textoAvisoTiempo.innerHTML = `<strong>Aviso de Cierre:</strong> Tu bloque se recortará a <strong>${textoCierre}</strong> de uso porque la biblioteca cierra a las <strong>${horaCierreTexto} hrs</strong>.`;
        avisoTiempoRecortado.classList.remove('d-none');
        btnConfirmarFinal.disabled = !chkTerminos.checked;
        return;
    }

    // Si no entra en ninguna restricción, el estado del botón depende únicamente de los términos
    btnConfirmarFinal.disabled = !chkTerminos.checked;
}
