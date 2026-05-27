using FishNet.Object;
using TMPro;
using UnityEngine;

public class PlayerView : NetworkBehaviour
{
    [SerializeField] private PlayerNetwork _playerNetwork;
    [SerializeField] private PlayerShooting _playerShooting;
    [SerializeField] private TMP_Text _nicknameText;
    [SerializeField] private TMP_Text _hpText;
    [SerializeField] private TMP_Text _ammoText;

    public override void OnStartClient()
    {
        if (_playerNetwork == null) _playerNetwork = GetComponent<PlayerNetwork>();
        if (_playerShooting == null) _playerShooting = GetComponent<PlayerShooting>();
    }

    private void Update()
    {
        if (_playerNetwork == null) return;

        if (_nicknameText) _nicknameText.text = _playerNetwork.Nickname.Value;
        if (_hpText) _hpText.text = $"HP: {_playerNetwork.HP.Value}";
        if (_ammoText && _playerShooting)
            _ammoText.text = $"Ammo: {_playerShooting.CurrentAmmo.Value}/{_playerShooting._maxAmmo}";
    }
}