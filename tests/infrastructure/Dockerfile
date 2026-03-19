FROM ubuntu:24.04

ENV DEBIAN_FRONTEND=noninteractive

# ── Base packages ──────────────────────────────────────────────────────
RUN apt-get update && apt-get install -y --no-install-recommends \
    curl \
    jq \
    dosfstools \
    mtools \
    openssh-client \
    sshpass \
    apt-transport-https \
    gnupg \
    lsb-release \
    ca-certificates \
    python3 \
    xorriso \
    && rm -rf /var/lib/apt/lists/*

# ── Microsoft repo (PowerShell) ───────────────────────────────────────
RUN curl -fsSL https://packages.microsoft.com/keys/microsoft.asc \
    | gpg --dearmor -o /usr/share/keyrings/microsoft-archive-keyring.gpg && \
    echo "deb [arch=amd64 signed-by=/usr/share/keyrings/microsoft-archive-keyring.gpg] \
    https://packages.microsoft.com/ubuntu/24.04/prod noble main" \
    > /etc/apt/sources.list.d/microsoft-prod.list

# ── HashiCorp repo (Terraform) ────────────────────────────────────────
RUN curl -fsSL https://apt.releases.hashicorp.com/gpg \
    | gpg --dearmor -o /usr/share/keyrings/hashicorp-archive-keyring.gpg && \
    echo "deb [arch=amd64 signed-by=/usr/share/keyrings/hashicorp-archive-keyring.gpg] \
    https://apt.releases.hashicorp.com noble main" \
    > /etc/apt/sources.list.d/hashicorp.list

# ── Proxmox repo (proxmox-auto-install-assistant) ─────────────────────
RUN curl -fsSL https://enterprise.proxmox.com/debian/proxmox-release-bookworm.gpg \
    -o /usr/share/keyrings/proxmox-release-bookworm.gpg && \
    echo "deb [signed-by=/usr/share/keyrings/proxmox-release-bookworm.gpg] \
    http://download.proxmox.com/debian/pve bookworm pve-no-subscription" \
    > /etc/apt/sources.list.d/proxmox-pve.list

# ── Install tooling ───────────────────────────────────────────────────
RUN apt-get update && apt-get install -y --no-install-recommends \
    powershell \
    terraform \
    proxmox-auto-install-assistant \
    && rm -rf /var/lib/apt/lists/*

# ── Install Pester ────────────────────────────────────────────────────
RUN pwsh -NoProfile -Command \
    'Set-PSRepository -Name PSGallery -InstallationPolicy Trusted; \
     Install-Module -Name Pester -MinimumVersion 5.0 -Scope AllUsers -Force'

# ── Verify all tools are present ──────────────────────────────────────
RUN echo "=== Tool verification ===" && \
    pwsh --version && \
    terraform --version && \
    proxmox-auto-install-assistant --version && \
    sshpass -V | head -1 && \
    xorriso --version 2>&1 | head -1 && \
    curl --version | head -1 && \
    jq --version && \
    python3 --version && \
    ssh -V 2>&1 && \
    echo "=== All tools verified ==="
