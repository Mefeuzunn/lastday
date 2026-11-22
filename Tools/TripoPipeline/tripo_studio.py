#!/usr/bin/env python3
"""
Tripo AI 3D Model Generation Pipeline (OpenAPI v3 Native)
Automated pipeline for generating low-poly game-ready 3D models with optional auto-rigging
and integrating them into Unity 6 URP.
"""

import os
import sys
import json
import time
import ssl
import argparse
import urllib.request
import urllib.error
from pathlib import Path

# Optional requests / dotenv imports with pure standard library fallback
try:
    import requests
    HAS_REQUESTS = True
except ImportError:
    HAS_REQUESTS = False

try:
    from dotenv import load_dotenv
    HAS_DOTENV = True
except ImportError:
    HAS_DOTENV = False


def load_env_file(project_root: Path):
    """Loads .env file from project root using dotenv or manual parsing."""
    env_path = project_root / ".env"
    if not env_path.exists():
        return

    if HAS_DOTENV:
        load_dotenv(env_path)
    else:
        with open(env_path, "r", encoding="utf-8") as f:
            for line in f:
                line = line.strip()
                if not line or line.startswith("#") or "=" not in line:
                    continue
                key, val = line.split("=", 1)
                key = key.strip()
                val = val.strip().strip("\"'")
                if key and key not in os.environ:
                    os.environ[key] = val


class InsufficientBalanceError(Exception):
    """Raised when Tripo account balance or credits are depleted."""
    pass


class TripoStudio:
    def __init__(self, api_key: str, base_url: str = "https://openapi.tripo3d.ai/v3"):
        self.api_key = api_key
        self.base_url = base_url.rstrip("/")

        try:
            self.ssl_context = ssl.create_default_context()
        except Exception:
            self.ssl_context = ssl._create_unverified_context()

        self.ssl_context.check_hostname = False
        self.ssl_context.verify_mode = ssl.CERT_NONE

        if not self.api_key or self.api_key == "BURAYA_TSK_ANAHTARINI_YAZ" or self.api_key == "BURAYA_API_ANAHTARINI_YAZ":
            raise ValueError(
                "Geçerli bir TRIPO_API_KEY bulunamadı. Lütfen proje kök dizinindeki .env dosyasını açıp "
                "TRIPO_API_KEY=tsk_... şeklinde aktif anahtarınızı girin."
            )

        if self.api_key.startswith("tcli_"):
            raise ValueError(
                f"Girdiğiniz anahtar ('{self.api_key[:8]}...') bir Client ID'dir (tcli_...).\n"
                "Tripo 3D API doğrudan model üretimi için 'tsk_' ile başlayan Secret Key (API Key) talep etmektedir.\n"
                "Lütfen https://developers.tripo3d.ai adresinde 'API Keys' sekmesinden "
                "'tsk_' ile başlayan Secret Key'i kopyalayıp .env dosyasına yazın."
            )

    def _http_request(self, method: str, endpoint: str, data: dict = None) -> dict:
        url = f"{self.base_url}{endpoint}"
        headers = {
            "Authorization": f"Bearer {self.api_key}",
            "Content-Type": "application/json",
            "User-Agent": "TripoStudio-Antigravity/2.0"
        }

        json_bytes = json.dumps(data).encode("utf-8") if data is not None else None

        req = urllib.request.Request(url, data=json_bytes, headers=headers, method=method)
        try:
            with urllib.request.urlopen(req, timeout=60, context=self.ssl_context) as response:
                resp_text = response.read().decode("utf-8")
                return json.loads(resp_text)
        except urllib.error.HTTPError as e:
            err_body = e.read().decode("utf-8", errors="replace")
            if e.code in (402, 403) or "credit" in err_body.lower() or "balance" in err_body.lower():
                raise InsufficientBalanceError(f"Kredi veya bakiye yetersiz ({e.code}): {err_body}")
            raise Exception(f"HTTP Hatası {e.code}: {e.reason}\nDetay: {err_body}")
        except Exception as e:
            raise Exception(f"Ağ isteği hatası ({url}): {e}")

    def check_balance(self) -> float:
        """Checks API credit balance on developers.tripo3d.ai."""
        try:
            res = self._http_request("GET", "/account/balance")
            balance = float(res.get("data", {}).get("balance", 0.0))
            return balance
        except Exception as e:
            print(f"⚠️ Bakiye kontrolü yapılamadı: {e}")
            return -1.0

    def create_text_to_model_task(self, prompt: str, face_limit: int = 10000) -> str:
        payload = {
            "prompt": prompt,
            "model": "v2.5-20250123"
        }
        res = self._http_request("POST", "/generation/text-to-model", payload)
        code = res.get("code", -1)
        if code != 0:
            msg = res.get("message", "Bilinmeyen hata")
            if "credit" in msg.lower() or "balance" in msg.lower():
                raise InsufficientBalanceError(f"Kredi yetersiz: {msg}")
            raise Exception(f"Görev oluşturulamadı (Kod {code}): {msg}")

        task_id = res.get("data", {}).get("task_id")
        if not task_id:
            raise Exception(f"Yanıtta task_id bulunamadı: {res}")
        return task_id

    def create_rig_task(self, original_task_id: str) -> str:
        payload = {
            "input": original_task_id,
            "rig_type": "biped",
            "spec": "unity",
            "out_format": "glb"
        }
        res = self._http_request("POST", "/animations/rig", payload)
        code = res.get("code", -1)
        if code != 0:
            msg = res.get("message", "Rig hatası")
            raise Exception(f"Auto-rig görevi oluşturulamadı: {msg}")
        return res.get("data", {}).get("task_id")

    def poll_task(self, task_id: str, label: str = "Model", timeout_seconds: int = 600) -> dict:
        start_time = time.time()
        last_progress = -1

        while time.time() - start_time < timeout_seconds:
            res = self._http_request("GET", f"/tasks/{task_id}")
            data = res.get("data", {})
            status = data.get("status", "").lower()
            progress = data.get("progress", 0)

            if progress != last_progress:
                print(f"  [{label}] İlerleme: %{progress} (Durum: {status})")
                last_progress = progress

            if status == "success":
                return data
            elif status in ("failed", "cancelled"):
                msg = res.get("message") or data.get("message") or "Bilinmeyen hata"
                raise Exception(f"Görev başarısız oldu ({status}): {msg}")

            time.sleep(2.5)

        raise TimeoutError(f"[{label}] Görevi {timeout_seconds} saniyede tamamlanamadı (Zaman Aşımı).")

    def download_file(self, url: str, destination_path: Path):
        destination_path.parent.mkdir(parents=True, exist_ok=True)
        req = urllib.request.Request(url, headers={"User-Agent": "TripoStudio-Antigravity/2.0"})
        with urllib.request.urlopen(req, timeout=120, context=self.ssl_context) as response, open(destination_path, "wb") as out_file:
            total_size = int(response.headers.get("content-length", 0))
            downloaded = 0
            block_size = 64 * 1024

            while True:
                buffer = response.read(block_size)
                if not buffer:
                    break
                downloaded += len(buffer)
                out_file.write(buffer)

        size_kb = destination_path.stat().st_size / 1024
        print(f"  ✓ Dosya başarıyla kaydedildi: {destination_path.name} ({size_kb:.1f} KB)")

    def generate_asset(self, name: str, prompt: str, rig: bool, face_limit: int, output_dir: Path) -> Path:
        print(f"\n==================================================")
        print(f"🚀 BAŞLATILIYOR: '{name}'")
        print(f"   Prompt: \"{prompt}\"")
        print(f"   Rig: {rig} | Poligon Limiti: {face_limit}")
        print(f"==================================================")

        out_path = output_dir / f"{name}.glb"

        # 1. Base Model Task
        print(f"  [1/3] Metinden 3D model üretimi başlatılıyor...")
        task_id = self.create_text_to_model_task(prompt, face_limit)
        print(f"  -> Task ID: {task_id}")

        model_data = self.poll_task(task_id, label=name)
        output_info = model_data.get("output", {})
        final_url = output_info.get("pbr_model") or output_info.get("model") or output_info.get("base_model")

        # 2. Auto-Rigging (If Character)
        if rig:
            print(f"  [2/3] Otomatik iskeletlendirme (Auto-Rig) başlatılıyor...")
            try:
                rig_task_id = self.create_rig_task(task_id)
                print(f"  -> Rig Task ID: {rig_task_id}")
                rig_data = self.poll_task(rig_task_id, label=f"{name}_Rig")
                rig_output = rig_data.get("output", {})
                rigged_url = rig_output.get("pbr_model") or rig_output.get("model") or rig_output.get("animation")
                if rigged_url:
                    final_url = rigged_url
                    print(f"  ✓ Unity Humanoid iskeletlendirme başarılı!")
            except Exception as e:
                print(f"  ⚠️ Auto-rig başarısız oldu ({e}); ham model kullanılacak.")

        if not final_url:
            raise Exception("Model URL'si yanıtta bulunamadı!")

        # 3. Download
        print(f"  [3/3] GLB dosyası indiriliyor...")
        self.download_file(final_url, out_path)
        return out_path


# 10 Starter Assets defined in specification
STARTER_ASSETS = [
    {
        "name": "Survivor",
        "rig": True,
        "face_limit": 10000,
        "prompt": "post-apocalyptic young survivor wearing tactical backpack, hoodie and cargo pants, stylized low poly 3d mobile game character, standing in strict T-pose, arms straight out, clean topology, isolated single character"
    },
    {
        "name": "Zombie_Standard",
        "rig": True,
        "face_limit": 8000,
        "prompt": "stylized zombie in torn dirty shirt and jeans, pale green skin, stylized low poly 3d mobile game character, standing in strict T-pose, arms straight out, clean topology, single character"
    },
    {
        "name": "Zombie_Runner",
        "rig": True,
        "face_limit": 8000,
        "prompt": "thin agile fast zombie runner, reddish mutated skin, tattered athletic shorts, lean muscular build, stylized low poly 3d mobile game character, standing in strict T-pose, arms straight out, clean topology"
    },
    {
        "name": "Zombie_Screamer",
        "rig": True,
        "face_limit": 8000,
        "prompt": "creepy mutated screamer zombie with glowing purple veins on neck and chest, wide open mouth, ragged clothes, stylized low poly 3d mobile game character, standing in strict T-pose, arms straight out, clean topology"
    },
    {
        "name": "Zombie_Tank",
        "rig": True,
        "face_limit": 12000,
        "prompt": "massive muscular heavy brute zombie, dark grey armored skin, torn heavy pants, broad shoulders, stylized low poly 3d mobile game character, standing in strict T-pose, arms straight out, clean topology"
    },
    {
        "name": "Dr_Aras",
        "rig": True,
        "face_limit": 10000,
        "prompt": "injured middle-aged male doctor wearing stained white lab coat over blue scrubs, stylized low poly 3d mobile game character, standing in strict T-pose, arms straight out, clean topology"
    },
    {
        "name": "Loot_Crate",
        "rig": False,
        "face_limit": 4000,
        "prompt": "post-apocalyptic reinforced wooden and rusty metal military supply loot crate with latches, stylized low poly 3d game prop, clean geometry, single object"
    },
    {
        "name": "Ambulance",
        "rig": False,
        "face_limit": 8000,
        "prompt": "abandoned rusted post-apocalyptic ambulance van with broken headlights and dirt stains, stylized low poly 3d game vehicle, single solid object, no interior"
    },
    {
        "name": "Car_Wreck",
        "rig": False,
        "face_limit": 8000,
        "prompt": "abandoned rusty sedan car with flat tires and smashed windows, post-apocalyptic stylized low poly 3d game vehicle, single solid object"
    },
    {
        "name": "Barricade",
        "rig": False,
        "face_limit": 5000,
        "prompt": "makeshift post-apocalyptic street barricade made of rusty metal barrels, wooden planks and sandbags, stylized low poly 3d game prop, single connected asset"
    }
]


def main():
    parser = argparse.ArgumentParser(description="Tripo AI 3D Model Generation CLI (OpenAPI v3)")
    parser.add_argument("--batch-starter", action="store_true", help="10 temel başlangıç oyun varlığını sırayla üretir.")
    parser.add_argument("--name", type=str, help="Tekil model adı (örn: Barricade)")
    parser.add_argument("--prompt", type=str, help="İngilizce 3D model promptu")
    parser.add_argument("--rig", action="store_true", help="Karakter modellerine otomatik iskelet (Auto-Rig) ekler")
    parser.add_argument("--face-limit", type=int, default=10000, help="Poligon sınırı (varsayılan: 10000)")
    parser.add_argument("--output-dir", type=str, default="Assets/_Project/Models/TripoGenerated", help="GLB modellerinin kaydedileceği klasör")
    args = parser.parse_args()

    project_root = Path(__file__).resolve().parent.parent.parent
    load_env_file(project_root)

    api_key = os.environ.get("TRIPO_API_KEY", "").strip()
    base_url = os.environ.get("TRIPO_BASE_URL", "https://openapi.tripo3d.ai/v3").strip()
    output_dir = project_root / args.output_dir

    try:
        studio = TripoStudio(api_key=api_key, base_url=base_url)
    except ValueError as e:
        print(f"\n❌ HATA: {e}\n")
        sys.exit(1)

    # Pre-check balance
    balance = studio.check_balance()
    print(f"\n[Tripo API] Doğrulama Başarılı! Mevcut Geliştirici Kredisi: {balance:.2f}")

    if balance == 0.0:
        print("\n" + "=" * 65)
        print("⚠️ DİKKAT: Tripo API Kredi Bakiyeniz '0.00' Olarak Görünüyor!")
        print("=" * 65)
        print("Tripo AI sisteminde Web Arayüzü Kredileri (tripo3d.ai) ile")
        print("Geliştirici API Kredileri (developers.tripo3d.ai) birbirinden ayrıdır.")
        print("API üzerinden üretim yapabilmek için:")
        print("1. https://developers.tripo3d.ai adresinde oturum açın.")
        print("2. Dashboard / Billing bölümünden geliştirici kredisi tanımlayın.")
        print("=" * 65 + "\n")

    completed_assets = []

    if args.batch_starter:
        print(f"\n=======================================================")
        print(f"🎯 TRIPO AI - 10 TEMEL OYUN VARLIĞI BAŞLANGIÇ PAKETİ")
        print(f"   Hedef Klasör: {output_dir}")
        print(f"=======================================================\n")

        for idx, item in enumerate(STARTER_ASSETS, 1):
            print(f"\n>>> [{idx}/{len(STARTER_ASSETS)}] {item['name']} üretiliyor...")
            try:
                saved_file = studio.generate_asset(
                    name=item["name"],
                    prompt=item["prompt"],
                    rig=item["rig"],
                    face_limit=item["face_limit"],
                    output_dir=output_dir
                )
                completed_assets.append((item["name"], str(saved_file)))
            except InsufficientBalanceError as ibe:
                print(f"\n⛔ DÖNGÜ DURDURULDU: {ibe}")
                print(f"Şimdiye kadar başarıyla üretilen modeller:")
                for name, path in completed_assets:
                    print(f" - {name}: {path}")
                break
            except Exception as e:
                print(f"⚠️ '{item['name']}' üretilirken hata oluştu: {e}")
                continue

        print(f"\n=======================================================")
        print(f"✅ İŞLEM RAPORU: {len(completed_assets)}/{len(STARTER_ASSETS)} varlık indirildi.")
        print(f"=======================================================\n")

    elif args.prompt and args.name:
        try:
            saved_file = studio.generate_asset(
                name=args.name,
                prompt=args.prompt,
                rig=args.rig,
                face_limit=args.face_limit,
                output_dir=output_dir
            )
            print(f"\n✅ Başarıyla tamamlandı: {saved_file}\n")
        except Exception as e:
            print(f"\n❌ HATA: {e}\n")
            sys.exit(1)
    else:
        parser.print_help()


if __name__ == "__main__":
    main()
