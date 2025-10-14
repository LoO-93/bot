using AutoBot.Models.Units;

namespace AutoBot.Models.LnMarkets;

public class UserModel
{
    public required string uid { get; set; }

    public required string role { get; set; }

    public Satoshi balance { get; set; }

    public required string username { get; set; }

    public Dollar synthetic_usd_balance { get; set; }

    public string? linkingpublickey { get; set; }

    public bool show_leaderboard { get; set; }

    public string? email { get; set; }

    public bool email_confirmed { get; set; }

    public bool use_taproot_addresses { get; set; }

    public string? account_type { get; set; }

    public bool auto_withdraw_enabled { get; set; }

    public string? auto_withdraw_lightning_address { get; set; }

    public string? nostr_pubkey { get; set; }

    public decimal fee_tier { get; set; }

    public bool totp_enabled { get; set; }

    public bool webauthn_enabled { get; set; }

    public object? metrics { get; set; }

    public UserModel Clone()
    {
        return new UserModel
        {
            uid = uid,
            role = role,
            balance = balance,
            username = username,
            synthetic_usd_balance = synthetic_usd_balance,
            linkingpublickey = linkingpublickey,
            show_leaderboard = show_leaderboard,
            email = email,
            email_confirmed = email_confirmed,
            use_taproot_addresses = use_taproot_addresses,
            account_type = account_type,
            auto_withdraw_enabled = auto_withdraw_enabled,
            auto_withdraw_lightning_address = auto_withdraw_lightning_address,
            nostr_pubkey = nostr_pubkey,
            fee_tier = fee_tier,
            totp_enabled = totp_enabled,
            webauthn_enabled = webauthn_enabled,
            metrics = metrics,
        };
    }
}
