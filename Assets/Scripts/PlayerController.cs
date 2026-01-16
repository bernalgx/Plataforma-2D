using System;
using System.Collections;
using UnityEngine;

// Controlador principal del Player
public class PlayerController : MonoBehaviour
{
	// Referencia al Rigidbody2D (físicas)
	private Rigidbody2D rb;

	// Dirección de movimiento (input horizontal/vertical)
	private Vector2 direccion;

	// Referencia al Animator
	private Animator anim;

	// =======================
	// ESTADÍSTICAS DEL PLAYER
	// =======================
	[Header("Estadisticas")]
	public float velocidadDeMovimiento = 20; // Velocidad al caminar
	public float fuerzaDeSalto = 10;           // Fuerza del salto
	public float velocidadDash = 50;          // Velocidad del dash
	public Vector2 direccionMovimiento;       // Dirección del movimiento para ataques

	// =======================
	// COLISIONES (SUELO)
	// =======================
	[Header("Colisiones")]
	public Vector2 abajo;          // Offset hacia abajo para detectar el suelo
	public float radioColision;    // Radio del círculo de detección
	public LayerMask layerPiso;    // Capa que cuenta como suelo

	// =======================
	// BOOLEANS DE ESTADO
	// =======================
	[Header("Booloeanos")]
	public bool PuedeMover = true; // Permite o bloquea movimiento
	public bool enSuelo = true;    // ¿Está tocando el suelo?
	public bool puedeDash;         // ¿Puede hacer dash?
	public bool haciendoDash;      // ¿Está actualmente en dash?
	public bool tocadoPiso;        // Controla el evento de aterrizaje
	public bool estaAtacando = false;         // ¿Está atacando?
											  // Se ejecuta una sola vez al crearse el objeto
	private void Awake()
	{
		// Obtenemos referencias a componentes
		rb = GetComponent<Rigidbody2D>();
		anim = GetComponent<Animator>();
	}

	// Se ejecuta al iniciar el juego
	void Start()
	{
		// Asegura que el player pueda moverse al inicio
		PuedeMover = true;
		puedeDash = true; // ← puede hacer dash al empezar
	}

	// Se ejecuta cada frame
	void Update()
	{
		Movimiento(); // Maneja input, animaciones y físicas
		Agarres();    // Detecta si el player está en el suelo
	}

	private void Atacar(Vector2 direccion)
	{
		if (Input.GetKeyDown(KeyCode.Z))
		{
			if (!estaAtacando && !haciendoDash)
			{
				anim.SetFloat("ataqueX", direccion.x);
				anim.SetFloat("ataqueY", direccion.y);

				anim.SetBool("atacar", true);
				//estaAtacando = true;

			}
		}

	}

	private void finalizarAtaque()
	{
		anim.SetBool("atacar", false);
		estaAtacando = false;
	}

	// =======================
	// DASH
	// =======================
	void Dash(float x, float y)
	{
		// Activa animación de dash
		anim.SetBool("dash", true);

		// Convierte la posición del player a coordenadas de viewport
		Vector3 posicionJugador = Camera.main.WorldToViewportPoint(transform.position);

		// Emite el efecto visual ripple
		Camera.main.GetComponent<RippleEffect>().Emit(posicionJugador);
		puedeDash = false;

		// Marca que puede dash (estado)
		puedeDash = true;

		// Resetea velocidad actual
		rb.velocity = Vector2.zero;

		// Aplica impulso normalizado en la dirección del input
		rb.velocity += new Vector2(x, y).normalized * velocidadDash;

		// Inicia la corrutina que controla la duración del dash
		StartCoroutine(PrepareDash());
	}

	// Controla el tiempo y la gravedad del dash
	private IEnumerator PrepareDash()
	{
		// Corrutina paralela para bloquear dash si toca el suelo
		StartCoroutine(DashSuelo());

		// Quita gravedad durante el dash
		rb.gravityScale = 0;
		haciendoDash = true;

		// Duración del dash
		yield return new WaitForSeconds(0.3f);

		// Restaura gravedad
		rb.gravityScale = 1;
		haciendoDash = false;

		// Finaliza animación de dash
		finalizarDash();
	}

	// Evita que el dash se pueda abusar al tocar el suelo
	IEnumerator DashSuelo()
	{
		yield return new WaitForSeconds(0.15f);

		if (enSuelo)
		{
			puedeDash = false;
		}

		puedeDash = false;
		anim.SetBool("dash", false);
	}

	// Llamado desde código o Animation Event
	public void finalizarDash()
	{
		anim.SetBool("dash", false);
	}

	// Se ejecuta una sola vez cuando el player toca el suelo
	private void TocarPiso()
	{
		puedeDash = true;
		haciendoDash = false;
		anim.SetBool("saltar", false);
	}

	// =======================
	// MOVIMIENTO GENERAL
	// =======================
	private void Movimiento()
	{
		// Input suave
		float x = Input.GetAxis("Horizontal");
		float y = Input.GetAxis("Vertical");

		// Input crudo (sin suavizado) para dash
		float xRaw = Input.GetAxisRaw("Horizontal");
		float yRaw = Input.GetAxisRaw("Vertical");

		// Dirección actual
		direccion = new Vector2(x, y);
		Vector2 direccionRaw = new Vector2(xRaw, yRaw);
		// Movimiento horizontal
		Caminar();

		Atacar(DireccionAtaque(direccionMovimiento, direccionRaw));

		// Mejora la sensación del salto
		MejorarSalto();

		// Salto
		if (Input.GetKeyDown(KeyCode.Space))
		{
			if (enSuelo)
			{
				anim.SetBool("saltar", true);
				Saltar();
			}
		}

		// Dash
		if (Input.GetKeyDown(KeyCode.X) && !haciendoDash && puedeDash)
		{
			float dir = Mathf.Sign(transform.localScale.x);
			Dash(dir, 0);
		}




		// Detecta aterrizaje
		if (enSuelo && !tocadoPiso)
		{
			TocarPiso();
			tocadoPiso = true;
		}

		if (!enSuelo && tocadoPiso)
		{
			tocadoPiso = false;

		}

		// Velocidad vertical para animaciones
		float velocidad;
		if (rb.velocity.y > 0)
			velocidad = 1;
		else
			velocidad = -1;

		if (!enSuelo)
		{
			anim.SetFloat("velocidadVertical", velocidad);
		}
		else
		{
			if (velocidad == -1)
				finalizarSalto();
		}
	}

	private Vector2 DireccionAtaque(Vector2 direccionMovimiento, Vector2 direccion)
	{
		if (rb.velocity.x == 0 && direccion.y != 0)
		{
			return new Vector2(0, direccion.y);
		}

		return new Vector2(direccionMovimiento.x, direccion.y);


	}

	// Finaliza animación de salto
	public void finalizarSalto()
	{
		anim.SetBool("saltar", false);
	}

	// =======================
	// DETECCIÓN DE SUELO
	// =======================
	private void Agarres()
	{
		enSuelo = Physics2D.OverlapCircle(
			((Vector2)transform.position + abajo),
			radioColision,
			layerPiso
		);
	}

	// =======================
	// MEJORA DE SALTO (FEEL)
	// =======================
	private void MejorarSalto()
	{
		// Caída más rápida
		if (rb.velocity.y < 0)
		{
			rb.velocity += Vector2.up * Physics2D.gravity.y * (2.5f - 1) * Time.deltaTime;
		}
		// Salto más corto si sueltas el botón
		else if (rb.velocity.y > 0 && !Input.GetKey(KeyCode.Space))
		{
			rb.velocity += Vector2.up * Physics2D.gravity.y * (2.0f - 1) * Time.deltaTime;
		}
	}

	// Aplica la fuerza de salto
	private void Saltar()
	{
		rb.velocity = new Vector2(rb.velocity.x, 0);
		rb.velocity += Vector2.up * fuerzaDeSalto;
	}

	// Movimiento horizontal + animaciones
	private void Caminar()
	{
		if (PuedeMover && !haciendoDash)
		{
			rb.velocity = new Vector2(direccion.x * velocidadDeMovimiento, rb.velocity.y);

			if (direccion != Vector2.zero)
			{
				if (enSuelo)
				{
					anim.SetBool("caminar", true);
				}
				else
				{
					anim.SetBool("saltar", true);
				}

				// Flip del sprite
				if (direccion.x < 0 && transform.localScale.x > 0)
				{
					direccionMovimiento = DireccionAtaque(Vector2.left, direccion);


					transform.localScale = new Vector3(
						-transform.localScale.x,
						transform.localScale.y,
						transform.localScale.z
					);
				}
				else if (direccion.x > 0 && transform.localScale.x < 0)
				{
					direccionMovimiento = DireccionAtaque(Vector2.right, direccion);
					transform.localScale = new Vector3(
						Mathf.Abs(transform.localScale.x),
						transform.localScale.y,
						transform.localScale.z
					);
				}
			}
			else
			{
				if (direccion.y > 0 && direccion.x == 0)
				{
					direccionMovimiento = DireccionAtaque(direccion, Vector2.up);
				}
				anim.SetBool("caminar", false);
			}
		}
	}
}