#!/bin/bash
# sample install script for Ubuntu 16.04, run as root
# run at your own risk, this is more to illustrate the process than be a production deploy script.

PAPIKA_USER=papika
INSTALL_DIR=/srv/papika

# create the papika user
mkdir -p ${INSTALL_DIR}
useradd -d ${INSTALL_DIR} -M -N -g nogroup -s /bin/false ${PAPIKA_USER}
chown ${PAPIKA_USER} ${INSTALL_DIR}

# set up postgres and role for papika
apt install -y postgresql postgresql-contrib
sudo -u postgres createuser ${PAPIKA_USER}

# checkout the server code
# do this funky bs because we want to clone into an existing directory
pushd ${INSTALL_DIR}
sudo -u ${PAPIKA_USER} git clone --no-checkout https://github.com/edbutler/papika-telemetry.git temp
sudo -u ${PAPIKA_USER} mv temp/.git .
rmdir temp
sudo -u ${PAPIKA_USER} git reset --hard HEAD

# install dependencies in a virtual env
cd server
apt install -y g++ python3-dev python3-venv postgresql-server-dev-all
sudo -Hu ${PAPIKA_USER} python3 -m venv venv
sudo -Hu ${PAPIKA_USER} venv/bin/pip install --upgrade setuptools pip
sudo -Hu ${PAPIKA_USER} venv/bin/pip install -r requirements.txt
popd

