FROM ubuntu:latest
#FROM amazonlinux:latest

RUN apt-get update -y && apt-get install -y zsh curl git
# RUN yum update -y && yum install -y zsh curl git

RUN useradd -ms /bin/zsh server

USER server
WORKDIR /home/server

RUN sh -c "$(curl -fsSL https://raw.github.com/ohmyzsh/ohmyzsh/master/tools/install.sh)"

COPY --chown=server Builds/Linux .
RUN chmod +x Voxelfield
# CMD ./Voxelfield
CMD zsh

EXPOSE 7777/udp
EXPOSE 7777/tcp
