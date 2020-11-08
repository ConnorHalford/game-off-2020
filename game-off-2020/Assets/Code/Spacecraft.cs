﻿using UnityEngine;

public class Spacecraft : MonoBehaviour
{
	public enum Model
	{
		CargoA,
		CargoB,
		Miner,
		Racer,
		SpeederA,
		SpeederB,
		SpeederC,
		SpeederD
	}

	[SerializeField] private Model _model = Model.CargoA;
	[SerializeField] private GameObject[] _modelGOs = new GameObject[System.Enum.GetValues(typeof(Model)).Length];
	[SerializeField] private Transform _modelRoot = null;

	[Header("Height")]
	[SerializeField] private float _heightWhenDriving = 0.5f;
	[SerializeField] private float _heightWhenParked = 0.1f;
	[SerializeField] private float _levitationSpeedUp = 5.0f;
	[SerializeField] private float _levitationSpeedDown = 1.0f;

	[Header("Turning")]
	[SerializeField] private float _turnSpeed = 5.0f;
	[SerializeField] private float _rollAmount = 1.0f;

	private Rigidbody _rb = null;
	private bool _driving = false;
	private float _targetHeight = 0.0f;
	private Vector2 _moveInput = Vector2.zero;
	private Model _modelPrevFrame = Model.CargoA;

	private void Awake()
	{
		_rb = GetComponent<Rigidbody>();
		ChangeModel(_model);
	}

	private void Update()
	{
		// Change model
		if (_model != _modelPrevFrame)
		{
			ChangeModel(_model);
		}

		// Start/stop driving
		if (Globals.Controls.Character.EnterVehicle.triggered)
		{
			if (_driving)
			{
				StopDriving();
			}
			else
			{
				StartDriving();
			}
		}

		// Read input
		_moveInput = Vector2.zero;
		if (_driving)
		{
			_moveInput = Globals.Controls.Character.Movement.ReadValue<Vector2>();
		}
	}

	private void FixedUpdate()
	{
		// Levitate
		float y = _rb.position.y;
		float deltaY = Time.deltaTime * (y < _targetHeight ? _levitationSpeedUp : _levitationSpeedDown);
		y = Mathf.SmoothStep(y, _targetHeight, deltaY);
		_rb.position = new Vector3(_rb.position.x, y, _rb.position.z);

		// Rotate
		if (_driving)
		{
			float turn = _moveInput.x * _turnSpeed * Time.deltaTime;
			_rb.angularVelocity += turn * Vector3.up;
		}
		Quaternion roll = Quaternion.Euler(0.0f, 0.0f, _rb.angularVelocity.y * _rollAmount * Time.deltaTime);
		_modelRoot.localRotation = roll;
	}

	private void StartDriving()
	{
		_targetHeight = _heightWhenDriving;
		_driving = true;
	}

	private void StopDriving()
	{
		_targetHeight = _heightWhenParked;
		_driving = false;
	}

	private void ChangeModel(Model model)
	{
		_model = model;
		_modelPrevFrame = _model;
		_modelGOs[(int)Model.CargoA].SetActive(_model == Model.CargoA);
		_modelGOs[(int)Model.CargoB].SetActive(_model == Model.CargoB);
		_modelGOs[(int)Model.Miner].SetActive(_model == Model.Miner);
		_modelGOs[(int)Model.Racer].SetActive(_model == Model.Racer);
		_modelGOs[(int)Model.SpeederA].SetActive(_model == Model.SpeederA);
		_modelGOs[(int)Model.SpeederB].SetActive(_model == Model.SpeederB);
		_modelGOs[(int)Model.SpeederC].SetActive(_model == Model.SpeederC);
		_modelGOs[(int)Model.SpeederD].SetActive(_model == Model.SpeederD);
	}
}
