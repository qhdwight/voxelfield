FROM ubuntu:latest

RUN apt-get update -y && apt-get install -y software-properties-common && \
    add-apt-repository ppa:git-core/ppa && apt-get update -y && \
    apt-get install -y git zsh curl

RUN useradd -ms /bin/zsh server

USER server
WORKDIR /home/server

RUN sh -c "$(curl -fsSL https://raw.github.com/ohmyzsh/ohmyzsh/master/tools/install.sh)"

# COPY --chown=server .ssh .ssh
COPY --chown=server Builds/Voxelfield/Linux .
RUN chmod +x Voxelfield
CMD ./Voxelfield -batchmode -nographics

EXPOSE 7777/udp
EXPOSE 7777/tcp
